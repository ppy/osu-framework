// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// 一个可绘制项，将目标可绘制子树渲染到内部帧缓冲区，
    /// 应用模糊，并在此可绘制项的绘制矩形内显示结果。
    /// </summary>
    public partial class BackdropBlurDrawable : Drawable, IBufferedDrawable
    {
        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(2, null, pixelSnapping: true, clipToRootNode: true);

        // 临时复用容器与代理，用于按排除列表构建捕获子树，避免捕获上层元素导致残影。
        private readonly Container captureTempContainer = new Container();

        private readonly List<ProxyCaptureDrawable> proxyPool = new List<ProxyCaptureDrawable>();
        private readonly List<Drawable> captureSources = new List<Drawable>();
        private readonly HashSet<Drawable> captureSourceSet = new HashSet<Drawable>();
        private int activeProxyCount;
        private IBackdropCaptureSourceProvider captureSourceProvider;

        private bool captureInProgress;
        private long updateVersion;
        private int frameCounter;

        #region 属性

        private bool effectEnabled;

        /// <summary>
        /// 是否启用背景捕获与模糊处理。
        /// 关闭后将不再生成捕获节点，可显著降低开销。
        /// </summary>
        public bool EffectEnabled
        {
            get => effectEnabled;
            set
            {
                if (effectEnabled == value)
                    return;

                effectEnabled = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Vector2 blurSigma;

        /// <summary>
        /// 控制两个正交方向上的模糊强度。
        /// </summary>
        [UsedImplicitly]
        public Vector2 BlurSigma
        {
            get => blurSigma;
            set
            {
                if (blurSigma == value)
                    return;

                blurSigma = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float blurRotation;

        /// <summary>
        /// 顺时针旋转模糊核，单位为度。
        /// </summary>
        [UsedImplicitly]
        public float BlurRotation
        {
            get => blurRotation;
            set
            {
                if (blurRotation == value)
                    return;

                blurRotation = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Vector2 frameBufferScale = new Vector2(0.75f);

        /// <summary>
        /// 内部帧缓冲区相对于此可绘制项大小的缩放比例。
        /// 较低的值会降低开销但会牺牲画质。
        /// </summary>
        [UsedImplicitly]
        public Vector2 FrameBufferScale
        {
            get => frameBufferScale;
            set
            {
                if (frameBufferScale == value)
                    return;

                frameBufferScale = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        /// <summary>
        /// 单个模糊目标。
        /// 当未提供 <see cref="CaptureSourceProvider"/> 或 <see cref="CaptureTargets"/> 时使用。
        /// </summary>
        public Drawable CaptureTarget { get; set; }

        /// <summary>
        /// 显式指定的多个模糊来源。
        /// 当非空时将按顺序捕获整组来源，优先级低于 <see cref="CaptureSourceProvider"/>，高于 <see cref="CaptureTarget"/>。
        /// </summary>
        public List<Drawable> CaptureTargets { get; } = new List<Drawable>();

        /// <summary>
        /// 动态提供显式模糊来源。
        /// 当存在且返回非空列表时，优先级高于 <see cref="CaptureTargets"/>。
        /// </summary>
        public IBackdropCaptureSourceProvider CaptureSourceProvider
        {
            get => captureSourceProvider;
            set
            {
                if (ReferenceEquals(captureSourceProvider, value))
                    return;

                if (captureSourceProvider != null)
                    captureSourceProvider.SourcesChanged -= onCaptureSourceProviderChanged;

                captureSourceProvider = value;

                if (captureSourceProvider != null)
                    captureSourceProvider.SourcesChanged += onCaptureSourceProviderChanged;

                invalidateCaptureSources();
            }
        }

        /// <summary>
        /// 捕获间隔（以帧计）。默认每 4 帧捕获一次以减少开销并降低 GC 压力。
        /// 设置为 1 表示每帧捕获（最高开销），设置为 0 则会被视为 1。
        /// </summary>
        public int CaptureFrameInterval { get; set; } = 4;

        /// <summary>
        /// 最大捕获频率（每秒），用于基于时间的节流。设置为 0 表示不基于时间限制。
        /// </summary>
        public int MaxCapturesPerSecond { get; set; } = 4;

#endregion

        private long lastCaptureMs;

        private IShader textureShader;
        private IShader blurShader;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
        }

        protected override void Update()
        {
            base.Update();

            if (!EffectEnabled || !hasCaptureSourceConfiguration())
                return;

            frameCounter++;
            int interval = Math.Max(1, CaptureFrameInterval);
            if (frameCounter % interval != 0)
                return;

            // 基于时间的节流：限制每秒最大的捕获次数以避免瞬时高开销。
            if (MaxCapturesPerSecond > 0)
            {
                long now = Environment.TickCount64;
                int minIntervalMs = 1000 / Math.Max(1, MaxCapturesPerSecond);
                if (now - lastCaptureMs < minIntervalMs)
                    return;

                lastCaptureMs = now;
            }

            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            if (!EffectEnabled || !hasCaptureSourceConfiguration())
                return null;

            if (captureInProgress)
                return null;

            DrawNode targetDrawNode;

            captureInProgress = true;

            try
            {
                if (getExplicitCaptureSources() is IReadOnlyList<Drawable> explicitCaptureSources)
                {
                    captureSources.Clear();
                    captureSourceSet.Clear();
                    resetCaptureTempContainer();

                    for (int i = 0; i < explicitCaptureSources.Count; i++)
                    {
                        Drawable source = explicitCaptureSources[i];

                        if (source == null)
                            continue;

                        appendExplicitCaptureSource(source);
                    }

                    if (captureSources.Count > 0)
                    {
                        populateCaptureTempContainer(captureSources);

                        targetDrawNode = captureTempContainer.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
                    }
                    else
                        targetDrawNode = null;

                    if (targetDrawNode == null)
                        return null;
                }
                else
                {
                    if (CaptureTarget == null)
                        return null;

                    targetDrawNode = CaptureTarget.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

                    if (targetDrawNode == null)
                        return null;
                }
            }
            finally
            {
                captureInProgress = false;
            }

            var drawNode = new BackdropBlurDrawNode(this, targetDrawNode, sharedData);
            drawNode.ApplyState();
            return drawNode;
        }

        private bool isExcludedRoot(Drawable drawable) => drawable == this;

        private IReadOnlyList<Drawable> getExplicitCaptureSources()
        {
            if (captureSourceProvider?.CaptureSources?.Count > 0)
                return captureSourceProvider.CaptureSources;

            return CaptureTargets.Count > 0 ? CaptureTargets : null;
        }

        private bool hasCaptureSourceConfiguration()
            => CaptureTarget != null || getExplicitCaptureSources() != null;

        private void onCaptureSourceProviderChanged() => invalidateCaptureSources();

        private void invalidateCaptureSources()
        {
            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        private void populateCaptureTempContainer(List<Drawable> sources)
        {
            resetCaptureTempContainer();

            activeProxyCount = sources.Count;

            for (int i = 0; i < sources.Count; i++)
            {
                ProxyCaptureDrawable proxy = getOrCreateProxy(i);
                proxy.SourceDrawable = sources[i];
                captureTempContainer.Add(proxy);
            }
        }

        private void resetCaptureTempContainer()
        {
            for (int i = 0; i < activeProxyCount; i++)
                proxyPool[i].SourceDrawable = null;

            activeProxyCount = 0;

            if (captureTempContainer.Count > 0)
                captureTempContainer.Clear(false);
        }

        private ProxyCaptureDrawable getOrCreateProxy(int index)
        {
            while (proxyPool.Count <= index)
                proxyPool.Add(new ProxyCaptureDrawable());

            return proxyPool[index];
        }

        private Drawable canonicaliseCaptureSource(Drawable drawable)
        {
            // ProxyDrawable 在单独捕获树中可能拿不到已验证的原始 DrawNode，导致透明。
            // 这里统一解到 Original 以提升稳定性。
            Drawable source = drawable.Original;
            return source ?? drawable;
        }

        private void appendExplicitCaptureSource(Drawable source)
        {
            if (source == null)
                return;

            Drawable canonical = canonicaliseCaptureSource(source);

            if (canonical != this && !isExcludedRoot(canonical) && captureSourceSet.Add(canonical))
                captureSources.Add(canonical);
        }

        private sealed partial class ProxyCaptureDrawable : Drawable
        {
            public Drawable SourceDrawable { get; set; }

            public ProxyCaptureDrawable()
            {
                RelativeSizeAxes = Axes.None;
            }

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode) => SourceDrawable?.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
        }

        IShader ITexturedShaderDrawable.TextureShader => textureShader;
        Color4 IBufferedDrawable.BackgroundColour => new Color4(0, 0, 0, 0);
        DrawColourInfo? IBufferedDrawable.FrameBufferDrawColour => new DrawColourInfo(Color4.White);
        Vector2 IBufferedDrawable.FrameBufferScale => frameBufferScale;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // 禁用效果防止后续捕获
            effectEnabled = false;

            // 清理 shader 引用
            textureShader = null;
            blurShader = null;

            // 清理捕获目标引用
            CaptureTarget = null;
            CaptureSourceProvider = null;

            // 先禁用效果再清理，防止清理过程中触发捕获
            if (CaptureTargets.Count > 0)
                CaptureTargets.Clear();

            resetCaptureTempContainer();

            if (captureTempContainer.Count > 0)
                captureTempContainer.Clear(true);

            proxyPool.Clear();

            // 清理集合
            captureSources.Clear();
            captureSourceSet.Clear();

            // 处置 sharedData
            sharedData.Dispose();
        }

        private class BackdropBlurDrawNode : BufferedDrawNode
        {
            protected new BackdropBlurDrawable Source => (BackdropBlurDrawable)base.Source;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;
            private long updateVersion;

            private IShader blurShader;

            // Reusable structs to avoid per-frame allocations
            private BlurParameters blurParameters;

            public BackdropBlurDrawNode(BackdropBlurDrawable source, DrawNode child, BufferedDrawNodeSharedData sharedData)
                : base(source, child, sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                updateVersion = Source.updateVersion;
                blurSigma = Source.BlurSigma;
                blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
                blurRotation = Source.BlurRotation;
                blurShader = Source.blurShader;
            }

            protected override long GetDrawVersion() => updateVersion;

            protected override void PopulateContents(IRenderer renderer)
            {
                base.PopulateContents(renderer);

                if (blurRadius.X <= 0 && blurRadius.Y <= 0)
                    return;

                renderer.PushScissorState(false);

                if (blurRadius.X > 0)
                    drawBlurredFrameBuffer(renderer, blurRadius.X, blurSigma.X, blurRotation);

                if (blurRadius.Y > 0)
                    drawBlurredFrameBuffer(renderer, blurRadius.Y, blurSigma.Y, blurRotation + 90);

                renderer.PopScissorState();
            }

            protected override void DrawContents(IRenderer renderer) => renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, DrawColourInfo.Colour);

            private void drawBlurredFrameBuffer(IRenderer renderer, int kernelRadius, float sigma, float rotation)
            {
                IFrameBuffer current = SharedData.CurrentEffectBuffer;
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    float radians = float.DegreesToRadians(rotation);

                    using (var blurParametersBuffer = renderer.CreateUniformBuffer<BlurParameters>())
                    {
                        // Update reusable struct instead of allocating new one
                        blurParameters.Radius = kernelRadius;
                        blurParameters.Sigma = sigma;
                        blurParameters.TexSize = current.Size;
                        blurParameters.Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));

                        blurParametersBuffer.Data = blurParameters;

                        blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                        blurShader.Bind();
                        renderer.DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                        blurShader.Unbind();
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct BlurParameters
            {
                public UniformVector2 TexSize;
                public UniformInt Radius;
                public UniformFloat Sigma;
                public UniformVector2 Direction;
                private readonly UniformPadding8 pad1;
            }
        }
    }
}
