// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    public partial class BackdropBlurContainer<T>
    {
        private class BackdropBlurDrawNode : BufferedDrawNode, ICompositeDrawNode
        {
            protected new BackdropBlurContainer<T> Source => (BackdropBlurContainer<T>)base.Source;

            protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            protected new BackdropBlurContainerDrawNodeSharedData SharedData => (BackdropBlurContainerDrawNodeSharedData)base.SharedData;

            private ColourInfo effectColour;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;

            private float maskCutoff;

            private IShader blurShader;
            private IShader textureMaskShader;

            private RectangleF parentDrawRect;

            private Vector2 effectBufferScale;
            private Vector2 effectBufferSize;

            public BackdropBlurDrawNode(BackdropBlurContainer<T> source, BackdropBlurContainerDrawNodeSharedData sharedData)
                : base(source, new CompositeDrawableDrawNode(source), sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                parentDrawRect = Source.lastParentDrawRect;

                effectColour = Source.EffectColour;

                effectBufferScale = Source.EffectBufferScale;
                effectBufferSize = new Vector2(MathF.Ceiling(DrawRectangle.Width * effectBufferScale.X), MathF.Ceiling(DrawRectangle.Height * effectBufferScale.Y));

                blurSigma = Source.BlurSigma;
                blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
                blurRotation = Source.BlurRotation;

                maskCutoff = Source.MaskCutoff;

                blurShader = Source.blurShader;
                textureMaskShader = Source.textureMaskShader;
            }

            protected override void PopulateContents(IRenderer renderer)
            {
                base.PopulateContents(renderer);

                if (blurRadius.X > 0 || blurRadius.Y > 0)
                {
                    renderer.PushScissorState(false);

                    renderer.PushDepthInfo(new DepthInfo(false));

                    if (blurRadius.X > 0) drawBlurredFrameBuffer(renderer, blurRadius.X, blurSigma.X, blurRotation);
                    if (blurRadius.Y > 0) drawBlurredFrameBuffer(renderer, blurRadius.Y, blurSigma.Y, blurRotation + 90);

                    renderer.PopDepthInfo();

                    renderer.PopScissorState();
                }
            }

            private IUniformBuffer<MaskParameters> maskParametersBuffer;

            protected override void DrawContents(IRenderer renderer)
            {
                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(effectColour);

                renderer.SetBlend(DrawColourInfo.Blending);

                if (blurRadius.X > 0 || blurRadius.Y > 0)
                {
                    maskParametersBuffer ??= renderer.CreateUniformBuffer<MaskParameters>();

                    maskParametersBuffer.Data = maskParametersBuffer.Data with
                    {
                        MaskCutoff = maskCutoff,
                    };

                    renderer.BindTexture(SharedData.MainBuffer.Texture, 1);

                    textureMaskShader.BindUniformBlock("m_MaskParameters", maskParametersBuffer);
                    textureMaskShader.Bind();
                    renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, finalEffectColour.MultiplyAlpha(DrawColourInfo.Colour));
                    textureMaskShader.Unbind();
                }

                base.DrawContents(renderer);
            }

            private IUniformBuffer<BlurParameters> blurParametersBuffer;

            private void drawBlurredFrameBuffer(IRenderer renderer, int kernelRadius, float sigma, float blurRotation)
            {
                blurParametersBuffer ??= renderer.CreateUniformBuffer<BlurParameters>();

                IFrameBuffer current = SharedData.GetCurrentSourceBuffer(out bool isBackBuffer);
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    float radians = float.DegreesToRadians(blurRotation);

                    blurParametersBuffer.Data = blurParametersBuffer.Data with
                    {
                        Radius = kernelRadius,
                        Sigma = sigma,
                        TexSize = current.Size,
                        Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians))
                    };

                    var rect = isBackBuffer
                        ? parentDrawRect.RelativeIn(DrawRectangle) * target.Size
                        : new RectangleF(0, 0, current.Texture.Width, current.Texture.Height);

                    blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                    blurShader.Bind();
                    renderer.DrawFrameBuffer(current, rect, ColourInfo.SingleColour(Color4.White));
                    blurShader.Unbind();
                }
            }

            protected override Vector2 GetFrameBufferSize(IFrameBuffer frameBuffer)
            {
                if (frameBuffer != SharedData.MainBuffer)
                    return effectBufferSize;

                return base.GetFrameBufferSize(frameBuffer);
            }

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;

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

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct MaskParameters
            {
                public UniformFloat MaskCutoff;
                private readonly UniformPadding12 pad1;
            }
        }

        private class BackdropBlurContainerDrawNodeSharedData : BufferedDrawNodeSharedData
        {
            public BackdropBlurContainerDrawNodeSharedData(RenderBufferFormat[] mainBufferFormats, bool pixelSnapping)
                : base(2, mainBufferFormats, pixelSnapping, true)
            {
            }

            public IFrameBuffer GetCurrentSourceBuffer(out bool isBackBuffer)
            {
                var buffer = CurrentEffectBuffer;

                if (buffer == MainBuffer && Renderer.FrameBuffer != null)
                {
                    isBackBuffer = true;
                    return Renderer.FrameBuffer;
                }

                isBackBuffer = false;
                return buffer;
            }
        }
    }
}
