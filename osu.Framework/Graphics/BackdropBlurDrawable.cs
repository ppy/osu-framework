// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        /// 捕获时需要排除的根节点（排除其整个子树）。
        /// 常用于排除位于模糊层上方的前景（如 note 层）。
        /// </summary>
        public List<Drawable> CaptureExclusions { get; } = new List<Drawable>();

        /// <summary>
        /// 捕获名称白名单（包含匹配，忽略大小写）。
        /// 当列表非空时，仅捕获名称或类型名匹配任一关键字的可绘制项（及其必要父容器展开后匹配的子项）。
        /// 例如：Background、BeatmapBackground。
        /// </summary>
        public List<string> CaptureIncludeNameFilters { get; } = new List<string>();

        /// <summary>
        /// 名称白名单是否匹配 Drawable.Name。
        /// </summary>
        public bool MatchIncludeFilterAgainstDrawableName { get; set; } = true;

        /// <summary>
        /// 名称白名单是否匹配 Drawable 的类型名。
        /// </summary>
        public bool MatchIncludeFilterAgainstTypeName { get; set; } = true;

        /// <summary>
        /// 名称白名单匹配模式。
        /// true: 包含匹配；false: 精确匹配。
        /// </summary>
        public bool IncludeNameFilterUseContainsMatch { get; set; } = true;

        /// <summary>
        /// 模糊目标。如果为 null，则使用包含此可绘制项的根可绘制项。
        /// </summary>
        public Drawable CaptureTarget { get; set; }

        /// <summary>
        /// 显式指定的多个模糊来源。
        /// 当非空时优先使用该列表进行捕获，不再依赖根树推断。
        /// </summary>
        public List<Drawable> CaptureTargets { get; } = new List<Drawable>();

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
                if (CaptureTargets.Count > 0)
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
                        Drawable fallbackTarget = CaptureTarget ?? findRoot();
                        if (fallbackTarget == null)
                            return null;

                        targetDrawNode = fallbackTarget.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

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

                        if (captureSources.Count == 0 && hasIncludeNameFilters)
                            collectFromWholeRoot(rootContainer, captureSources);

                        List<Drawable> sourcesToUse = captureSources;

                        if (sourcesToUse.Count == 0)
                        {
                            if (hasIncludeNameFilters)
                            {
                                captureTempContainer.Clear(false);
                                return null;
                            }

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

        private bool isExcludedRoot(Drawable drawable)
        {
            if (drawable == this)
                return true;

            for (int i = 0; i < CaptureExclusions.Count; i++)
            {
                if (CaptureExclusions[i] == drawable)
                    return true;
            }

            return false;
        }

        private bool hasIncludeNameFilters => CaptureIncludeNameFilters.Count > 0;

        private bool matchesIncludeFilter(Drawable drawable)
        {
            if (!hasIncludeNameFilters)
                return true;

            Drawable original = drawable.Original;

            string drawableName = MatchIncludeFilterAgainstDrawableName ? drawable.Name : null;
            string typeName = MatchIncludeFilterAgainstTypeName ? drawable.GetType().Name : null;

            string originalDrawableName = MatchIncludeFilterAgainstDrawableName ? original.Name : null;
            string originalTypeName = MatchIncludeFilterAgainstTypeName ? original.GetType().Name : null;

            for (int i = 0; i < CaptureIncludeNameFilters.Count; i++)
            {
                string filter = CaptureIncludeNameFilters[i];
                if (string.IsNullOrWhiteSpace(filter))
                    continue;

                if (IncludeNameFilterUseContainsMatch)
                {
                    if (drawableName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    if (typeName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    if (originalDrawableName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    if (originalTypeName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                else
                {
                    if (drawableName != null && string.Equals(drawableName, filter, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (typeName != null && string.Equals(typeName, filter, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (originalDrawableName != null && string.Equals(originalDrawableName, filter, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (originalTypeName != null && string.Equals(originalTypeName, filter, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

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

                appendFilteredCaptureSources(child, outList);
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

            Drawable canonical = canonicaliseCaptureSource(source);

            if (canonical != source && canonical != this && !isExcludedRoot(canonical) && captureSourceSet.Add(canonical))
                captureSources.Add(canonical);
        }

        private bool containsExcludedDescendant(Drawable drawable)
        {
            if (isExcludedRoot(drawable))
                return true;

            if (drawable is not Container container)
                return false;

            foreach (var child in container.Children)
            {
                if (containsExcludedDescendant(child))
                    return true;
            }

            return false;
        }

        private bool containsIncludedDescendant(Drawable drawable)
        {
            if (matchesIncludeFilter(drawable))
                return true;

            if (drawable is not Container container)
                return false;

            foreach (var child in container.Children)
            {
                if (containsIncludedDescendant(child))
                    return true;
            }

            return false;
        }

        private void appendFilteredCaptureSources(Drawable drawable, List<Drawable> outList)
        {
            if (isExcludedRoot(drawable))
                return;

            bool includeSelf = matchesIncludeFilter(drawable);
            bool includeByChildren = hasIncludeNameFilters && containsIncludedDescendant(drawable);

            if (drawable is Container container && (containsExcludedDescendant(drawable) || (!includeSelf && includeByChildren)))
            {
                foreach (var child in container.Children)
                    appendFilteredCaptureSources(child, outList);

                return;
            }

            if (!includeSelf)
                return;

            var source = canonicaliseCaptureSource(drawable);

            if (captureSourceSet.Add(source))
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
