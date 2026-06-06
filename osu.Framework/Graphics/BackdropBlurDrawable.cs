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
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// 局部毛玻璃容器：把指定的"捕获源"子树渲染进内部帧缓冲区，模糊后在自身绘制矩形内显示。
    /// 用于实现任意局部区域虚化（毛玻璃卡片透出下层模糊内容）。
    /// </summary>
    /// <remarks>
    /// 关键设计：捕获源在它们各自的正常位置仍照常渲染（本类不会接管/禁用其渲染），本类只是
    /// 通过 <see cref="Drawable.GenerateDrawNodeSubtree"/> 取得它们"当帧已生成"的 draw node 并
    /// 复用绘制（<c>forceNewDrawNode: false</c>），因此不会重复分配整棵子树的 draw node，也支持
    /// 同一源被多个 <see cref="BackdropBlurDrawable"/> 同时捕获（如 mania 双 stage）。
    /// </remarks>
    public partial class BackdropBlurDrawable : Drawable, IBufferedDrawable
    {
        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(2, null, pixelSnapping: true, clipToRootNode: true);

        // 复用的源 draw node 列表，避免每帧分配。
        private readonly List<Drawable> resolvedSources = new List<Drawable>();
        private readonly HashSet<Drawable> resolvedSourceSet = new HashSet<Drawable>();

        private bool captureInProgress;
        private long updateVersion;
        private IBackdropCaptureSourceProvider captureSourceProvider;

        #region 属性

        private bool effectEnabled;

        /// <summary>
        /// 是否启用背景捕获与模糊处理。关闭后不再生成捕获节点，开销接近零。
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
        /// 内部帧缓冲区相对于此可绘制项大小的缩放比例。较低的值降低开销但牺牲画质。
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
        /// 单个模糊目标。当未提供 <see cref="CaptureSourceProvider"/> 或 <see cref="CaptureTargets"/> 时使用。
        /// </summary>
        public Drawable CaptureTarget { get; set; }

        /// <summary>
        /// 显式指定的多个模糊来源（从后到前）。优先级低于 <see cref="CaptureSourceProvider"/>，高于 <see cref="CaptureTarget"/>。
        /// </summary>
        public List<Drawable> CaptureTargets { get; } = new List<Drawable>();

        /// <summary>
        /// 动态提供显式模糊来源（从后到前）。存在且返回非空列表时优先级最高。
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

        #endregion

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

            // 每帧推进版本号 → 每帧重绘 FBO，确保与下层移动内容（视频/故事板）逐帧一致，无节流抖动。
            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            if (!EffectEnabled || !hasCaptureSourceConfiguration())
                return null;

            // 防止源子树意外包含本节点导致的递归。
            if (captureInProgress)
                return null;

            captureInProgress = true;

            List<DrawNode> sourceNodes;

            try
            {
                sourceNodes = resolveSourceNodes(frame, treeIndex);
            }
            finally
            {
                captureInProgress = false;
            }

            if (sourceNodes == null || sourceNodes.Count == 0)
                return null;

            var captureRoot = new CaptureRootDrawNode(this, sourceNodes);
            var drawNode = new BackdropBlurDrawNode(this, captureRoot, sharedData);
            drawNode.ApplyState();
            return drawNode;
        }

        /// <summary>
        /// 解析当前捕获源，按"复用已生成 draw node"的方式取得各源当帧的 draw node。
        /// </summary>
        private List<DrawNode> resolveSourceNodes(ulong frame, int treeIndex)
        {
            resolvedSources.Clear();
            resolvedSourceSet.Clear();

            if (getExplicitCaptureSources() is IReadOnlyList<Drawable> explicitSources)
            {
                for (int i = 0; i < explicitSources.Count; i++)
                    appendSource(explicitSources[i]);
            }
            else
                appendSource(CaptureTarget);

            if (resolvedSources.Count == 0)
                return null;

            var nodes = new List<DrawNode>(resolvedSources.Count);

            for (int i = 0; i < resolvedSources.Count; i++)
            {
                Drawable source = resolvedSources[i];

                if (!source.IsLoaded)
                    continue;

                // forceNewDrawNode: false → 复用源在本帧正常渲染时已生成的 draw node，不重复分配整棵子树。
                DrawNode node = source.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: false);

                if (node != null)
                    nodes.Add(node);
            }

            return nodes.Count > 0 ? nodes : null;
        }

        private void appendSource(Drawable source)
        {
            if (source == null)
                return;

            // 解到 Original 以兼容传入 proxy 的情形，并去重。
            Drawable canonical = source.Original ?? source;

            if (canonical != this && resolvedSourceSet.Add(canonical))
                resolvedSources.Add(canonical);
        }

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

        IShader ITexturedShaderDrawable.TextureShader => textureShader;
        Color4 IBufferedDrawable.BackgroundColour => new Color4(0, 0, 0, 0);
        DrawColourInfo? IBufferedDrawable.FrameBufferDrawColour => new DrawColourInfo(Color4.White);
        Vector2 IBufferedDrawable.FrameBufferScale => frameBufferScale;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // 禁用效果防止后续捕获。
            effectEnabled = false;

            textureShader = null;
            blurShader = null;

            CaptureTarget = null;
            CaptureSourceProvider = null;
            CaptureTargets.Clear();

            resolvedSources.Clear();
            resolvedSourceSet.Clear();

            // 仅在已初始化（曾被绘制过）时释放，避免在从未启用过效果时触发 Debug 断言。
            if (sharedData.IsInitialised)
                sharedData.Dispose();
        }

        /// <summary>
        /// 承载多个捕获源 draw node 的合成节点，作为 <see cref="BufferedDrawNode"/> 的单一 Child 喂入。
        /// </summary>
        /// <remarks>
        /// 子节点是各源"当帧已生成"的 draw node（drawNodes[treeIndex]），由三重缓冲保证跨线程绘制安全
        /// （与 <c>ProxyDrawNode</c> 的复用方式一致）。它们由各自的源拥有，本节点既不持有引用计数、
        /// 也不负责释放，仅在绘制阶段按从后到前的顺序复用绘制。
        /// </remarks>
        private sealed class CaptureRootDrawNode : DrawNode
        {
            private readonly List<DrawNode> children;

            public CaptureRootDrawNode(IDrawable source, List<DrawNode> children)
                : base(source)
            {
                this.children = children;
            }

            // 仅应用本合成节点自身状态（blend/depth 用），子节点状态由各源在生成阶段各自应用。
            // public override void ApplyState()
            // {
            //     base.ApplyState();
            // }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                for (int i = 0; i < children.Count; i++)
                    DrawOther(children[i], renderer);
            }
        }

        private class BackdropBlurDrawNode : BufferedDrawNode
        {
            protected new BackdropBlurDrawable Source => (BackdropBlurDrawable)base.Source;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;
            private long updateVersion;

            private IShader blurShader;

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
