﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using Rectangle = System.Drawing.Rectangle;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK;
using OpenTK.Graphics.ES30;
using OpenTK.Graphics;
using osu.Framework.Threading;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using System;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNode : ContainerDrawNode
    {
        public FrameBuffer[] FrameBuffers;
        public Color4 BackgroundColour;

        public Vector2 BlurSigma;
        public float BlurRotation;

        public Shader BlurShader;

        // If this counter contains a value larger then 0, then we have to redraw.
        public AtomicCounter ForceRedraw;

        public RectangleF ScreenSpaceDrawRectangle;
        public QuadBatch<TexturedVertex2D> Batch;
        public List<RenderbufferInternalFormat> Formats;

        private InvokeOnDisposal establishFrameBufferViewport(Vector2 roundedSize)
        {
            // Disable masking for generating the frame buffer since masking will be re-applied
            // when actually drawing later on anyways. This allows more information to be captured
            // in the frame buffer and helps with cached buffers being re-used.
            Rectangle screenSpaceMaskingRect = new Rectangle((int)Math.Floor(ScreenSpaceDrawRectangle.X), (int)Math.Floor(ScreenSpaceDrawRectangle.Y), (int)roundedSize.X + 1, (int)roundedSize.Y + 1);

            GLWrapper.PushScissor(new MaskingInfo
            {
                ScreenSpaceAABB = screenSpaceMaskingRect,
                MaskingRect = ScreenSpaceDrawRectangle,
                ToMaskingSpace = Matrix3.Identity,
                LinearBlendRange = 1.0f,
            }, true);

            // Match viewport to FrameBuffer such that we don't draw unnecessary pixels.
            GLWrapper.PushViewport(new Rectangle(0, 0, (int)roundedSize.X, (int)roundedSize.Y));

            return new InvokeOnDisposal(delegate
            {
                GLWrapper.PopViewport();
                GLWrapper.PopScissor();
            });
        }

        private InvokeOnDisposal bindFrameBuffer(FrameBuffer frameBuffer, Vector2 requestedSize)
        {
            if (!frameBuffer.IsInitialized)
                frameBuffer.Initialize();

            // These additional render buffers are only required if e.g. depth
            // or stencil information needs to also be stored somewhere.
            foreach (var f in Formats)
                frameBuffer.Attach(f);

            // This setter will also take care of allocating a texture of appropriate size within the framebuffer.
            frameBuffer.Size = requestedSize;

            frameBuffer.Bind();

            return new InvokeOnDisposal(() => frameBuffer.Unbind());
        }

        private void drawFrameBufferToBackBuffer(FrameBuffer frameBuffer, RectangleF drawRectangle)
        {
            // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
            RectangleF textureRect = new RectangleF(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);
            if (frameBuffer.Texture.Bind())
                // Color was already applied by base.Draw(); no need to re-apply. Thus we use White here.
                frameBuffer.Texture.Draw(drawRectangle, textureRect, DrawInfo.Colour);
        }

        private double evalGaussian(float x, float sigma)
        {
            const double INV_SQRT_2PI = 0.39894;
            return INV_SQRT_2PI * Math.Exp(-0.5 * x * x / (sigma * sigma)) / sigma;
        }

        private int findBlurRadius(float sigma)
        {
            const float GAUSS_THRESHOLD = 0.1f;
            const int MAX_RADIUS = 200;

            double center = evalGaussian(0, sigma);
            double threshold = GAUSS_THRESHOLD * center;
            for (int i = 0; i < MAX_RADIUS; ++i)
                if (evalGaussian(i, sigma) < threshold)
                    return Math.Max(i-1, 0);

            return MAX_RADIUS;
        }

        private void drawChildren(IVertexBatch vertexBatch, Vector2 frameBufferSize)
        {
            // Fill the frame buffer with drawn children
            using (bindFrameBuffer(currentFrameBuffer, frameBufferSize))
            {
                // We need to draw children as if they were zero-based to the top-left of the texture.
                // We can do this by adding a translation component to our (orthogonal) projection matrix.
                GLWrapper.PushOrtho(ScreenSpaceDrawRectangle);

                GLWrapper.ClearColour(BackgroundColour);
                base.Draw(vertexBatch);

                GLWrapper.PopOrtho();
            }
        }

        private void drawBlurredFrameBuffer(int kernelRadius, float sigma, float blurRotation)
        {
            FrameBuffer source = currentFrameBuffer;
            FrameBuffer target = advanceFrameBuffer();
        
            GLWrapper.SetBlend(new BlendingInfo
            {
                Source = BlendingFactorSrc.One,
                Destination = BlendingFactorDest.Zero,
                SourceAlpha = BlendingFactorSrc.One,
                DestinationAlpha = BlendingFactorDest.Zero,
            });

            using (bindFrameBuffer(target, source.Size))
            {
                BlurShader.GetUniform<int>(@"g_Radius").Value = kernelRadius;
                BlurShader.GetUniform<float>(@"g_Sigma").Value = sigma;
                BlurShader.GetUniform<Vector2>(@"g_TexSize").Value = source.Size;

                float radians = -MathHelper.DegreesToRadians(blurRotation);
                BlurShader.GetUniform<Vector2>(@"g_BlurDirection").Value = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));

                BlurShader.Bind();
                drawFrameBufferToBackBuffer(source, new RectangleF(0, 0, source.Texture.Width, source.Texture.Height));
                BlurShader.Unbind();
            }
        }

        private int currentFrameBufferIndex = 0;
        private FrameBuffer currentFrameBuffer => FrameBuffers[currentFrameBufferIndex];
        private FrameBuffer advanceFrameBuffer() => FrameBuffers[currentFrameBufferIndex = (currentFrameBufferIndex + 1) % 2];

        /// <summary>
        /// Makes sure the first frame buffer is always the one we want to draw from.
        /// This saves us the need to sync the draw indices across draw node trees
        /// since the FrameBuffers array is already shared.
        /// </summary>
        private void finalizeFrameBuffer()
        {
            if (currentFrameBufferIndex == 1)
            {
                FrameBuffer temp = FrameBuffers[0];
                FrameBuffers[0] = FrameBuffers[1];
                FrameBuffers[1] = temp;

                currentFrameBufferIndex = 0;
            }
        }

        public override void Draw(IVertexBatch vertexBatch)
        {
            if (ForceRedraw.Reset() > 0)
            {
                Vector2 frameBufferSize = new Vector2((float)Math.Ceiling(ScreenSpaceDrawRectangle.Width), (float)Math.Ceiling(ScreenSpaceDrawRectangle.Height));

                using (establishFrameBufferViewport(frameBufferSize))
                {
                    drawChildren(vertexBatch, frameBufferSize);

                    // Blur post-processing in case a blur radius is defined.
                    int radiusX = BlurSigma.X > 0 ? findBlurRadius(BlurSigma.X) : 0;
                    int radiusY = BlurSigma.Y > 0 ? findBlurRadius(BlurSigma.Y) : 0;

                    if (radiusX > 0 || radiusY > 0)
                    {
                        GL.Disable(EnableCap.ScissorTest);

                        if (radiusX > 0) drawBlurredFrameBuffer(radiusX, BlurSigma.X, BlurRotation);
                        if (radiusY > 0) drawBlurredFrameBuffer(radiusY, BlurSigma.Y, BlurRotation + 90);

                        GL.Enable(EnableCap.ScissorTest);
                    }
                }

                finalizeFrameBuffer();
            }

            // Blit the final framebuffer to screen.
            GLWrapper.SetBlend(DrawInfo.Blending);

            Shader.GetUniform<bool>(@"g_PremultiplyAlpha").Value = false;

            Shader.Bind();
            drawFrameBufferToBackBuffer(FrameBuffers[0], ScreenSpaceDrawRectangle);
            Shader.Unbind();

            Shader.GetUniform<bool>(@"g_PremultiplyAlpha").Value = true;
        }
    }
}
