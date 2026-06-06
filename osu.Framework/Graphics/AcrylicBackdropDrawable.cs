// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
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
    /// 真·全局毛玻璃（Acrylic）：内容无关地虚化"自身矩形正下方"已经渲染好的任意内容
    /// （图片 / 视频 / 故事板 / 其它 UI），无需知道下层是什么，也无需把下层作为捕获源传入。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 原理：渲染管线中，处于本节点之前（从后到前）的内容已经写入"当前绑定的离屏帧缓冲"
    /// （<see cref="IRenderer.CurrentFrameBuffer"/>）。本节点在自己被绘制时：
    /// 1. 绑定自己的效果缓冲，对当前帧缓冲按本卡片屏幕矩形取一块"切片"；
    /// 2. 高斯模糊该切片；
    /// 3. 把模糊结果绘制回原位。
    /// 因此实现了"毛玻璃卡片透出下层模糊内容"，且逐帧一致、零枚举、不影响其它绘制。
    /// </para>
    /// <para>
    /// 使用前提：本节点必须处在一个"离屏帧缓冲"之下，否则当前绑定目标是不可采样的 backbuffer
    /// （此时 <see cref="IRenderer.CurrentFrameBuffer"/> 为 null，本节点跳过绘制）。最简单的做法是把
    /// 需要被透视的整块内容包进一个全屏 <see cref="Containers.BufferedContainer"/>，再把本卡片作为它的
    /// 子节点放在任意位置。当前版本的几何映射假定该承载缓冲为"全屏、原点对齐、FrameBufferScale=1"，
    /// 这正是"全屏 HUD 任意拖动毛玻璃"的目标场景。
    /// </para>
    /// </remarks>
    public partial class AcrylicBackdropDrawable : Drawable, ITexturedShaderDrawable
    {
        // 两个效果缓冲用于横/纵两趟高斯模糊的乒乓。
        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(2, null, pixelSnapping: true, clipToRootNode: false);

        private bool effectEnabled = true;

        /// <summary>
        /// 是否启用模糊。关闭后本节点不产生绘制（开销接近零）。
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

        private Vector2 blurSigma = new Vector2(12);

        /// <summary>
        /// 两个正交方向上的模糊强度。
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

        private Vector2 frameBufferScale = new Vector2(0.5f);

        /// <summary>
        /// 内部效果缓冲相对于本卡片大小的缩放。较低的值降低开销但牺牲画质（模糊本身会掩盖降采样）。
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

        private IShader textureShader;
        private IShader blurShader;

        IShader ITexturedShaderDrawable.TextureShader => textureShader;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
        }

        protected override DrawNode CreateDrawNode() => new AcrylicBackdropDrawNode(this, sharedData);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            effectEnabled = false;
            textureShader = null;
            blurShader = null;

            if (sharedData.IsInitialised)
                sharedData.Dispose();
        }

        private class AcrylicBackdropDrawNode : TexturedShaderDrawNode
        {
            protected new AcrylicBackdropDrawable Source => (AcrylicBackdropDrawable)base.Source;

            protected readonly BufferedDrawNodeSharedData SharedData;

            private bool effectEnabled;
            private RectangleF screenSpaceDrawRectangle;
            private Vector2 frameBufferScale;
            private Vector2 frameBufferSize;
            private ColourInfo drawColour;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;

            private IShader blurShader;

            private BlurParameters blurParameters;

            public AcrylicBackdropDrawNode(AcrylicBackdropDrawable source, BufferedDrawNodeSharedData sharedData)
                : base(source)
            {
                SharedData = sharedData;
            }

            public override void ApplyState()
            {
                base.ApplyState();

                effectEnabled = Source.EffectEnabled;
                screenSpaceDrawRectangle = Source.ScreenSpaceDrawQuad.AABBFloat;
                frameBufferScale = Source.FrameBufferScale;
                frameBufferSize = new Vector2(
                    MathF.Ceiling(screenSpaceDrawRectangle.Width * frameBufferScale.X),
                    MathF.Ceiling(screenSpaceDrawRectangle.Height * frameBufferScale.Y));
                drawColour = DrawColourInfo.Colour;

                blurSigma = Source.BlurSigma;
                blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
                blurRotation = Source.BlurRotation;
                blurShader = Source.blurShader;
            }

            protected override void Draw(IRenderer renderer)
            {
                if (!effectEnabled || frameBufferSize.X < 1 || frameBufferSize.Y < 1)
                    return;

                // 当前绑定的离屏帧缓冲即"下层已渲染内容"。若直接渲染到 backbuffer（不可采样）则无法实现，跳过。
                IFrameBuffer scene = renderer.CurrentFrameBuffer;

                if (scene == null)
                    return;

                if (!SharedData.IsInitialised)
                    SharedData.Initialise(renderer);

                SharedData.ResetCurrentEffectBuffer();

                using (establishFrameBufferViewport(renderer))
                {
                    // 1) 取下层切片：把 scene 纹理按其全屏矩形绘制到本卡片的效果缓冲，
                    //    当前视口/正交只覆盖本卡片屏幕矩形，于是只保留卡片正下方那一块。
                    using (bindFrameBuffer(SharedData.MainBuffer))
                    {
                        renderer.PushOrtho(screenSpaceDrawRectangle);
                        renderer.Clear(new ClearInfo(new Color4(0, 0, 0, 0)));
                        renderer.SetBlend(BlendingParameters.None);

                        BindTextureShader(renderer);
                        renderer.DrawFrameBuffer(scene, new RectangleF(0, 0, scene.Texture.Width, scene.Texture.Height), ColourInfo.SingleColour(Color4.White));
                        UnbindTextureShader(renderer);

                        renderer.PopOrtho();
                    }

                    // 2) 对切片做高斯模糊（横/纵两趟）。
                    populateBlur(renderer);
                }

                // 3) 把模糊结果绘制回卡片原位。
                BindTextureShader(renderer);
                base.Draw(renderer);
                renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, screenSpaceDrawRectangle, drawColour);
                UnbindTextureShader(renderer);
            }

            private void populateBlur(IRenderer renderer)
            {
                if (blurRadius.X <= 0 && blurRadius.Y <= 0)
                    return;

                renderer.PushScissorState(false);

                if (blurRadius.X > 0)
                    drawBlurredFrameBuffer(renderer, blurRadius.X, blurSigma.X, blurRotation);

                if (blurRadius.Y > 0)
                    drawBlurredFrameBuffer(renderer, blurRadius.Y, blurSigma.Y, blurRotation + 90);

                renderer.PopScissorState();
            }

            private void drawBlurredFrameBuffer(IRenderer renderer, int kernelRadius, float sigma, float rotation)
            {
                IFrameBuffer current = SharedData.CurrentEffectBuffer;
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (bindFrameBuffer(target))
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

            private ValueInvokeOnDisposal<IFrameBuffer> bindFrameBuffer(IFrameBuffer frameBuffer)
            {
                // 设置 Size 会按需分配合适尺寸的纹理。
                frameBuffer.Size = frameBufferSize;
                frameBuffer.Bind();
                return new ValueInvokeOnDisposal<IFrameBuffer>(frameBuffer, static b => b.Unbind());
            }

            private ValueInvokeOnDisposal<(AcrylicBackdropDrawNode node, IRenderer renderer)> establishFrameBufferViewport(IRenderer renderer)
            {
                RectangleI screenSpaceMaskingRect = new RectangleI(
                    (int)Math.Floor(screenSpaceDrawRectangle.X),
                    (int)Math.Floor(screenSpaceDrawRectangle.Y),
                    (int)frameBufferSize.X + 1,
                    (int)frameBufferSize.Y + 1);

                renderer.PushMaskingInfo(new MaskingInfo
                {
                    ScreenSpaceAABB = screenSpaceMaskingRect,
                    MaskingRect = screenSpaceDrawRectangle,
                    ToMaskingSpace = Matrix3.Identity,
                    BlendRange = 1,
                    AlphaExponent = 1,
                }, true);

                renderer.PushViewport(new RectangleI(0, 0, (int)frameBufferSize.X, (int)frameBufferSize.Y));
                renderer.PushScissor(new RectangleI(0, 0, (int)frameBufferSize.X, (int)frameBufferSize.Y));
                renderer.PushScissorOffset(screenSpaceMaskingRect.Location);

                return new ValueInvokeOnDisposal<(AcrylicBackdropDrawNode node, IRenderer renderer)>((this, renderer), static tup => tup.node.returnViewport(tup.renderer));
            }

            private void returnViewport(IRenderer renderer)
            {
                renderer.PopScissorOffset();
                renderer.PopViewport();
                renderer.PopScissor();
                renderer.PopMaskingInfo();
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
