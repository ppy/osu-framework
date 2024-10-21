// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
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
    public class BackdropBlurDrawNode : BufferedDrawNode
    {
        public BackdropBlurDrawNode(IBufferedDrawable source, DrawNode child, BufferedDrawNodeSharedData sharedData)
            : base(source, child, sharedData)
        {
        }

        protected new IBackdropBlurDrawable Source => (IBackdropBlurDrawable)base.Source;

        private Vector2 blurSigma;
        private Vector2I blurRadius;
        private float blurRotation;

        private float maskCutoff;

        private IShader blurShader;
        private IShader textureMaskShader;

        private RectangleF backBufferDrawRect;

        private Vector2 effectBufferScale;
        private Vector2 effectBufferSize;

        private float backdropOpacity;
        private float backdropTintStrength;

        public override void ApplyState()
        {
            base.ApplyState();

            backBufferDrawRect = Source.LastBackBufferDrawRect;

            effectBufferScale = Source.EffectBufferScale;
            effectBufferSize = new Vector2(MathF.Ceiling(DrawRectangle.Width * effectBufferScale.X), MathF.Ceiling(DrawRectangle.Height * effectBufferScale.Y));

            blurSigma = Source.BlurSigma;
            blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
            blurRotation = Source.BlurRotation;

            maskCutoff = Source.MaskCutoff;
            backdropOpacity = Source.BackdropOpacity;
            backdropTintStrength = Source.BackdropTintStrength;

            blurShader = Source.BlurShader;
            textureMaskShader = Source.BackdropBlurShader;
        }

        protected override void PopulateContents(IRenderer renderer)
        {
            base.PopulateContents(renderer);

            // we need the intermediate blur pass in order to draw the final blending pass, so we always have to draw both passes.
            if ((blurRadius.X > 0 || blurRadius.Y > 0) && backdropOpacity > 0)
            {
                renderer.PushScissorState(false);

                renderer.PushDepthInfo(new DepthInfo(false));

                drawBlurredBackBuffer(renderer, blurRadius.X, blurSigma.X, blurRotation);

                renderer.PopDepthInfo();

                renderer.PopScissorState();
            }
        }

        private IUniformBuffer<BlurParameters> blurParametersBuffer;

        private void drawBlurredBackBuffer(IRenderer renderer, int kernelRadius, float sigma, float blurRotation)
        {
            blurParametersBuffer ??= renderer.CreateUniformBuffer<BlurParameters>();

            if (renderer.FrameBuffer == null)
                throw new InvalidOperationException("No frame buffer available to blur with.");

            IFrameBuffer current = renderer.FrameBuffer;
            IFrameBuffer target = SharedData.GetNextEffectBuffer();

            renderer.SetBlend(BlendingParameters.None);

            renderer.PushScissorState(false);

            renderer.PushDepthInfo(new DepthInfo(false));

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

                blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                blurShader.Bind();
                renderer.DrawFrameBuffer(current, backBufferDrawRect.RelativeIn(DrawRectangle) * target.Size, ColourInfo.SingleColour(Color4.White));
                blurShader.Unbind();
            }

            renderer.PopDepthInfo();

            renderer.PopScissorState();
        }

        protected override bool RequiresEffectBufferRedraw => true;

        protected override void DrawContents(IRenderer renderer)
        {
            renderer.SetBlend(DrawColourInfo.Blending);

            if ((blurRadius.X > 0 || blurRadius.Y > 0) && backdropOpacity > 0)
            {
                blurParametersBuffer ??= renderer.CreateUniformBuffer<BlurParameters>();

                float radians = float.DegreesToRadians(blurRotation + 90);

                blurParametersBuffer.Data = blurParametersBuffer.Data with
                {
                    Radius = blurRadius.Y,
                    Sigma = blurSigma.Y,
                    TexSize = SharedData.CurrentEffectBuffer.Size,
                    Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians)),
                    MaskCutoff = maskCutoff,
                    BlurResolution = effectBufferScale.Y,
                    BackdropOpacity = backdropOpacity,
                    BackdropTintStrength = backdropTintStrength,
                };

                renderer.BindTexture(SharedData.MainBuffer.Texture, 1);

                blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                textureMaskShader.Bind();
                renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, DrawColourInfo.Colour);
                textureMaskShader.Unbind();
            }
            else
            {
                base.DrawContents(renderer);
            }
        }

        protected override Vector2 GetFrameBufferSize(IFrameBuffer frameBuffer)
        {
            if (frameBuffer != SharedData.MainBuffer)
                return effectBufferSize;

            return base.GetFrameBufferSize(frameBuffer);
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
            public UniformFloat MaskCutoff;
            public UniformFloat BlurResolution;
            public UniformFloat BackdropOpacity;
            public UniformFloat BackdropTintStrength;
            public UniformPadding8 pad1;
        }
    }
}
