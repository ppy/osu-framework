// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Containers;
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

        private bool captureInProgress;
        private long updateVersion;
        private int frameCounter;

        private bool effectEnabled = true;

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

        // 临时复用容器与代理，用于按排除列表构建捕获子树，避免捕获上层元素导致残影。
        private readonly Container captureTempContainer = new Container();
        private readonly List<ProxyCaptureDrawable> proxyPool = new List<ProxyCaptureDrawable>();
        private readonly List<Drawable> captureSources = new List<Drawable>();
        private readonly HashSet<Drawable> captureSourceSet = new HashSet<Drawable>();

        /// <summary>
        /// 模糊目标。如果为 null，则使用包含此可绘制项的根可绘制项。
        /// </summary>
        public Drawable CaptureTarget { get; set; }

        /// <summary>
        /// 显式指定的多个模糊来源。
        /// 当非空时优先使用该列表进行捕获，不再依赖根树推断。
        /// </summary>
        public List<Drawable> CaptureTargets { get; } = new List<Drawable>();

        /// <summary>
        /// 严格显式捕获模式。
        /// 开启后，仅捕获 <see cref="CaptureTargets"/> 本体，且捕获失败时不回退到其它目标。
        /// </summary>
        public bool StrictCaptureTargetsMode { get; set; } = true;

        /// <summary>
        /// 穿透捕获模式（按绘制顺序）。
        /// 开启后忽略显式目标配置，仅捕获当前可绘制项下方（先绘制）的图层内容。
        /// </summary>
        public bool PassthroughByDrawOrder { get; set; }

        private Vector2 blurSigma;

        /// <summary>
        /// 控制两个正交方向上的模糊强度。
        /// </summary>
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

        private Vector2 frameBufferScale = new Vector2(0.25f);

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
        /// 捕获间隔（以帧计）。默认每 3 帧捕获一次以减少开销并降低 GC 压力。
        /// 设置为 1 表示每帧捕获（最高开销），设置为 0 则会被视为 1。
        /// </summary>
        public int CaptureFrameInterval { get; set; } = 6;

        /// <summary>
        /// 最大捕获频率（每秒），用于基于时间的节流。设置为 0 表示不基于时间限制。
        /// </summary>
        public int MaxCapturesPerSecond { get; set; } = 4;

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

            if (!EffectEnabled)
                return;

            frameCounter++;
            int interval = Math.Max(1, CaptureFrameInterval);
            if ((frameCounter % interval) != 0)
                return;

            // 基于时间的节流：限制每秒最大的捕获次数以避免瞬时高开销。
            if (MaxCapturesPerSecond > 0)
            {
                long now = Environment.TickCount64;
                int minIntervalMs = 1000 / Math.Max(1, MaxCapturesPerSecond);
                if ((now - lastCaptureMs) < minIntervalMs)
                    return;

                lastCaptureMs = now;
            }

            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            if (!EffectEnabled)
                return null;

            if (captureInProgress)
                return null;

            DrawNode targetDrawNode;

            captureInProgress = true;

            try
            {
                if (PassthroughByDrawOrder)
                {
                    targetDrawNode = generateLowerLayerCapture(frame, treeIndex);

                    if (targetDrawNode == null)
                        return null;
                }
                else if (CaptureTargets.Count > 0)
                {
                    captureSources.Clear();
                    captureSourceSet.Clear();
                    captureTempContainer.Clear(false);

                    for (int i = 0; i < CaptureTargets.Count; i++)
                    {
                        Drawable source = CaptureTargets[i];

                        if (source == null)
                            continue;

                        appendExplicitCaptureSource(source);
                    }

                    if (captureSources.Count > 0)
                    {
                        for (int i = 0; i < captureSources.Count; i++)
                        {
                            ProxyCaptureDrawable proxy;

                            if (i < proxyPool.Count)
                            {
                                proxy = proxyPool[i];
                                proxy.SourceDrawable = captureSources[i];
                            }
                            else
                            {
                                proxy = new ProxyCaptureDrawable(captureSources[i]);
                                proxyPool.Add(proxy);
                            }

                            captureTempContainer.Add(proxy);
                        }

                        targetDrawNode = captureTempContainer.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
                    }
                    else
                        targetDrawNode = null;

                    if (targetDrawNode == null)
                    {
                        if (StrictCaptureTargetsMode)
                            return null;

                        targetDrawNode = generateLowerLayerCapture(frame, treeIndex);

                        if (targetDrawNode == null && CaptureTarget != null)
                            targetDrawNode = CaptureTarget.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

                        if (targetDrawNode == null)
                            return null;
                    }
                }
                else
                {
                    Drawable target = CaptureTarget ?? findRoot();
                    if (target == null)
                        return null;

                    if (CaptureTarget == null && target is Container rootContainer)
                    {
                        captureSources.Clear();
                        captureSourceSet.Clear();
                        captureTempContainer.Clear(false);

                        collectBefore(rootContainer, this, captureSources);

                        List<Drawable> sourcesToUse = captureSources;

                        if (sourcesToUse.Count == 0)
                        {
                            targetDrawNode = target.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
                            if (targetDrawNode == null)
                                return null;
                        }
                        else
                        {
                            for (int i = 0; i < sourcesToUse.Count; i++)
                            {
                                ProxyCaptureDrawable proxy;

                                if (i < proxyPool.Count)
                                {
                                    proxy = proxyPool[i];
                                    proxy.SourceDrawable = sourcesToUse[i];
                                }
                                else
                                {
                                    proxy = new ProxyCaptureDrawable(sourcesToUse[i]);
                                    proxyPool.Add(proxy);
                                }

                                captureTempContainer.Add(proxy);
                            }

                            targetDrawNode = captureTempContainer.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

                            if (targetDrawNode == null)
                                return null;
                        }
                    }
                    else
                    {
                        targetDrawNode = target.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

                        if (targetDrawNode == null)
                            return null;
                    }
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

        private static bool isAncestorOf(Drawable node, Drawable descendant)
        {
            while (descendant != null)
            {
                if (descendant == node)
                    return true;

                descendant = descendant.Parent;
            }

            return false;
        }

        private bool collectBefore(Container container, Drawable stopAt, List<Drawable> outList)
        {
            foreach (var child in container.Children)
            {
                if (child == stopAt)
                    return true;

                if (isAncestorOf(child, stopAt))
                {
                    if (child is Container c)
                    {
                        if (collectBefore(c, stopAt, outList))
                            return true;

                        continue;
                    }

                    return true;
                }

                if (isExcludedRoot(child))
                    continue;

                appendFilteredCaptureSources(child, outList);
            }

            return false;
        }

        private void collectFromWholeRoot(Container container, List<Drawable> outList)
        {
            foreach (var child in container.Children)
            {
                if (child == this)
                    continue;

                if (isAncestorOf(child, this))
                {
                    if (child is Container c)
                        collectBefore(c, this, outList);

                    continue;
                }

                if (isExcludedRoot(child))
                    continue;

                outList.Add(child);
            }
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

            if (source != this && !isExcludedRoot(source) && captureSourceSet.Add(source))
                captureSources.Add(source);

            if (StrictCaptureTargetsMode)
                return;

            Drawable canonical = canonicaliseCaptureSource(source);

            if (canonical != source && canonical != this && !isExcludedRoot(canonical) && captureSourceSet.Add(canonical))
                captureSources.Add(canonical);
        }

        private DrawNode generateLowerLayerCapture(ulong frame, int treeIndex)
        {
            Container rootContainer = findTraversalRootContainer();

            captureSources.Clear();
            captureSourceSet.Clear();
            captureTempContainer.Clear(false);

            if (rootContainer != null)
                collectBefore(rootContainer, this, captureSources);
            else
                collectBeforeInParentChain(this, captureSources);

            if (captureSources.Count == 0)
                return null;

            for (int i = 0; i < captureSources.Count; i++)
            {
                ProxyCaptureDrawable proxy;

                if (i < proxyPool.Count)
                {
                    proxy = proxyPool[i];
                    proxy.SourceDrawable = captureSources[i];
                }
                else
                {
                    proxy = new ProxyCaptureDrawable(captureSources[i]);
                    proxyPool.Add(proxy);
                }

                captureTempContainer.Add(proxy);
            }

            return captureTempContainer.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
        }

        private void collectBeforeInParentChain(Drawable start, List<Drawable> outList)
        {
            Drawable current = start;

            while (current.Parent != null)
            {
                if (current.Parent is Container parentContainer)
                {
                    foreach (var sibling in parentContainer.Children)
                    {
                        if (sibling == current)
                            break;

                        if (isExcludedRoot(sibling))
                            continue;

                        appendFilteredCaptureSources(sibling, outList);
                    }
                }

                current = current.Parent;
            }
        }

        private Container findTraversalRootContainer()
        {
            Container highestContainer = null;

            Drawable current = this;

            while (current.Parent != null)
            {
                current = current.Parent;

                if (current is Container container)
                    highestContainer = container;
            }

            return highestContainer;
        }

        private void appendFilteredCaptureSources(Drawable drawable, List<Drawable> outList)
        {
            if (isExcludedRoot(drawable))
                return;

            if (captureSourceSet.Add(drawable))
                outList.Add(drawable);

            var source = canonicaliseCaptureSource(drawable);

            if (source != drawable && captureSourceSet.Add(source))
                outList.Add(source);
        }

        private sealed partial class ProxyCaptureDrawable : Drawable
        {
            public Drawable SourceDrawable { get; set; }

            public ProxyCaptureDrawable(Drawable source)
            {
                SourceDrawable = source;
                RelativeSizeAxes = Axes.None;
            }

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
                => SourceDrawable?.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);
        }

        private Drawable findRoot()
        {
            Drawable target = this;

            while (target.Parent != null)
                target = target.Parent;

            return target;
        }

        IShader ITexturedShaderDrawable.TextureShader => textureShader;
        Color4 IBufferedDrawable.BackgroundColour => new Color4(0, 0, 0, 0);
        DrawColourInfo? IBufferedDrawable.FrameBufferDrawColour => new DrawColourInfo(Color4.White);
        Vector2 IBufferedDrawable.FrameBufferScale => frameBufferScale;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
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
            private IUniformBuffer<BlurParameters> blurParametersBuffer;

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

            protected override void DrawContents(IRenderer renderer)
                => renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, DrawColourInfo.Colour);

            private void drawBlurredFrameBuffer(IRenderer renderer, int kernelRadius, float sigma, float rotation)
            {
                blurParametersBuffer ??= renderer.CreateUniformBuffer<BlurParameters>();

                IFrameBuffer current = SharedData.CurrentEffectBuffer;
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    float radians = float.DegreesToRadians(rotation);

                    blurParametersBuffer.Data = blurParametersBuffer.Data with
                    {
                        Radius = kernelRadius,
                        Sigma = sigma,
                        TexSize = current.Size,
                        Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians))
                    };

                    blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                    blurShader.Bind();
                    renderer.DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                    blurShader.Unbind();
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                blurParametersBuffer?.Dispose();
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
