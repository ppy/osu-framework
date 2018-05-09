// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK;
using OpenTK.Graphics.ES30;
using OpenTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using System.Diagnostics;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNodeSharedData
    {
        /// <summary>
        /// The <see cref="Shader"/> to use when rendering blur effects.
        /// </summary>
        public Shader BlurShader;

        /// <summary>
        /// The <see cref="FrameBuffer"/>s to render to.
        /// These are used in a ping-pong manner to render effects <see cref="BufferedContainerDrawNode"/>.
        /// </summary>
        public readonly FrameBuffer[] FrameBuffers = new FrameBuffer[3];

        /// <summary>
        /// The version of drawn contents currently present in <see cref="FrameBuffers"/>.
        /// This should only be modified by <see cref="BufferedContainerDrawNode"/>.
        /// </summary>
        public long DrawVersion = -1;

        public BufferedContainerDrawNodeSharedData()
        {
            for (int i = 0; i < FrameBuffers.Length; i++)
                FrameBuffers[i] = new FrameBuffer();
        }
    }

    public class BufferedContainerDrawNode : CompositeDrawNode
    {
        public bool DrawOriginal;
        public Color4 BackgroundColour;
        public ColourInfo EffectColour;
        public BlendingParameters EffectBlending;
        public EffectPlacement EffectPlacement;

        public Vector2 BlurSigma;
        public Vector2I BlurRadius;
        public float BlurRotation;

        public long UpdateVersion;

        public RectangleF ScreenSpaceDrawRectangle;
        public All FilteringMode;

        /// <summary>
        /// The <see cref="RenderbufferInternalFormat"/>s to use when drawing children.
        /// </summary>
        public readonly List<RenderbufferInternalFormat> Formats = new List<RenderbufferInternalFormat>();

        public new BufferedContainerDrawNodeSharedData Shared;

        /// <summary>
        /// Whether this <see cref="BufferedContainerDrawNode"/> should have its children re-drawn.
        /// </summary>
        public bool RequiresRedraw => UpdateVersion > Shared.DrawVersion;

        private InvokeOnDisposal establishFrameBufferViewport(Vector2 roundedSize)
        {
            // Disable masking for generating the frame buffer since masking will be re-applied
            // when actually drawing later on anyways. This allows more information to be captured
            // in the frame buffer and helps with cached buffers being re-used.
            RectangleI screenSpaceMaskingRect = new RectangleI((int)Math.Floor(ScreenSpaceDrawRectangle.X), (int)Math.Floor(ScreenSpaceDrawRectangle.Y), (int)roundedSize.X + 1, (int)roundedSize.Y + 1);

            GLWrapper.PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = screenSpaceMaskingRect,
                MaskingRect = ScreenSpaceDrawRectangle,
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
            }, true);

            // Match viewport to FrameBuffer such that we don't draw unnecessary pixels.
            GLWrapper.PushViewport(new RectangleI(0, 0, (int)roundedSize.X, (int)roundedSize.Y));

            return new InvokeOnDisposal(delegate
            {
                GLWrapper.PopViewport();
                GLWrapper.PopMaskingInfo();
            });
        }

        private InvokeOnDisposal bindFrameBuffer(FrameBuffer frameBuffer, Vector2 requestedSize)
        {
            if (!frameBuffer.IsInitialized)
                frameBuffer.Initialize(true, FilteringMode);

            // These additional render buffers are only required if e.g. depth
            // or stencil information needs to also be stored somewhere.
            foreach (var f in Formats)
                frameBuffer.Attach(f);

            // This setter will also take care of allocating a texture of appropriate size within the framebuffer.
            frameBuffer.Size = requestedSize;

            frameBuffer.Bind();

            return new InvokeOnDisposal(frameBuffer.Unbind);
        }

        private void drawFrameBufferToBackBuffer(FrameBuffer frameBuffer, RectangleF drawRectangle, ColourInfo colourInfo)
        {
            // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
            RectangleF textureRect = new RectangleF(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);
            if (frameBuffer.Texture.Bind())
                // Color was already applied by base.Draw(); no need to re-apply. Thus we use White here.
                frameBuffer.Texture.DrawQuad(drawRectangle, textureRect, colourInfo);
        }

        private void drawChildren(Action<TexturedVertex2D> vertexAction, Vector2 frameBufferSize)
        {
            // Fill the frame buffer with drawn children
            using (bindFrameBuffer(currentFrameBuffer, frameBufferSize))
            {
                // We need to draw children as if they were zero-based to the top-left of the texture.
                // We can do this by adding a translation component to our (orthogonal) projection matrix.
                GLWrapper.PushOrtho(ScreenSpaceDrawRectangle);

                GLWrapper.ClearColour(BackgroundColour);
                base.Draw(vertexAction);

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
                Shared.BlurShader.GetUniform<int>(@"g_Radius").Value = kernelRadius;
                Shared.BlurShader.GetUniform<float>(@"g_Sigma").Value = sigma;
                Shared.BlurShader.GetUniform<Vector2>(@"g_TexSize").Value = source.Size;

                float radians = -MathHelper.DegreesToRadians(blurRotation);
                Shared.BlurShader.GetUniform<Vector2>(@"g_BlurDirection").Value = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));

                Shared.BlurShader.Bind();
                drawFrameBufferToBackBuffer(source, new RectangleF(0, 0, source.Texture.Width, source.Texture.Height), ColourInfo.SingleColour(Color4.White));
                Shared.BlurShader.Unbind();
            }
        }

        private int currentFrameBufferIndex;
        private FrameBuffer currentFrameBuffer => Shared.FrameBuffers[currentFrameBufferIndex];
        private FrameBuffer advanceFrameBuffer() => Shared.FrameBuffers[currentFrameBufferIndex = (currentFrameBufferIndex + 1) % 2];

        /// <summary>
        /// Makes sure the first frame buffer is always the one we want to draw from.
        /// This saves us the need to sync the draw indices across draw node trees
        /// since the Shared.FrameBuffers array is already shared.
        /// </summary>
        private void finalizeFrameBuffer()
        {
            if (currentFrameBufferIndex != 0)
            {
                Trace.Assert(currentFrameBufferIndex == 1,
                    $"Only the first two framebuffers should be the last to be written to at the end of {nameof(Draw)}.");

                FrameBuffer temp = Shared.FrameBuffers[0];
                Shared.FrameBuffers[0] = Shared.FrameBuffers[1];
                Shared.FrameBuffers[1] = temp;

                currentFrameBufferIndex = 0;
            }
        }

        // Our effects will be drawn into framebuffers 0 and 1. If we want to preserve the originally
        // drawn children we need to put them in a separate buffer; in this case buffer 2. Otherwise,
        // we do not want to allocate a third buffer for nothing and hence we start with 0.
        private int originalIndex => DrawOriginal && (BlurRadius.X > 0 || BlurRadius.Y > 0) ? 2 : 0;

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            currentFrameBufferIndex = originalIndex;

            Vector2 frameBufferSize = new Vector2((float)Math.Ceiling(ScreenSpaceDrawRectangle.Width), (float)Math.Ceiling(ScreenSpaceDrawRectangle.Height));
            if (RequiresRedraw)
            {
                Shared.DrawVersion = UpdateVersion;

                using (establishFrameBufferViewport(frameBufferSize))
                {
                    drawChildren(vertexAction, frameBufferSize);

                    // Blur post-processing in case a blur radius is defined.
                    if (BlurRadius.X > 0 || BlurRadius.Y > 0)
                    {
                        GL.Disable(EnableCap.ScissorTest);

                        if (BlurRadius.X > 0) drawBlurredFrameBuffer(BlurRadius.X, BlurSigma.X, BlurRotation);
                        if (BlurRadius.Y > 0) drawBlurredFrameBuffer(BlurRadius.Y, BlurSigma.Y, BlurRotation + 90);

                        GL.Enable(EnableCap.ScissorTest);
                    }
                }

                finalizeFrameBuffer();
            }

            RectangleF drawRectangle = FilteringMode == All.Nearest
                ? new RectangleF(ScreenSpaceDrawRectangle.X, ScreenSpaceDrawRectangle.Y, frameBufferSize.X, frameBufferSize.Y)
                : ScreenSpaceDrawRectangle;

            Shader.Bind();

            if (DrawOriginal && EffectPlacement == EffectPlacement.InFront)
            {
                GLWrapper.SetBlend(DrawInfo.Blending);
                drawFrameBufferToBackBuffer(Shared.FrameBuffers[originalIndex], drawRectangle, DrawInfo.Colour);
            }

            // Blit the final framebuffer to screen.
            GLWrapper.SetBlend(new BlendingInfo(EffectBlending));

            ColourInfo effectColour = DrawInfo.Colour;
            effectColour.ApplyChild(EffectColour);
            drawFrameBufferToBackBuffer(Shared.FrameBuffers[0], drawRectangle, effectColour);

            if (DrawOriginal && EffectPlacement == EffectPlacement.Behind)
            {
                GLWrapper.SetBlend(DrawInfo.Blending);
                drawFrameBufferToBackBuffer(Shared.FrameBuffers[originalIndex], drawRectangle, DrawInfo.Colour);
            }

            Shader.Unbind();
        }
    }
}
