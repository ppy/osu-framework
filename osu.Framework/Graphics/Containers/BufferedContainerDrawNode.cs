// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK;
using osuTK.Graphics.ES30;
using osuTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using System.Diagnostics;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNodeSharedData : IDisposable
    {
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

        ~BufferedContainerDrawNodeSharedData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            for (int i = 0; i < FrameBuffers.Length; i++)
                FrameBuffers[i].Dispose();
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

        /// <summary>
        /// The <see cref="IShader"/> to use when rendering blur effects.
        /// </summary>
        public IShader BlurShader;

        public readonly BufferedContainerDrawNodeSharedData SharedData;

        public BufferedContainerDrawNode(BufferedContainerDrawNodeSharedData sharedData)
        {
            SharedData = sharedData;
        }

        public override void ApplyFromDrawable(Drawable source)
        {
            base.ApplyFromDrawable(source);

            var container = (BufferedContainer)source;

            ScreenSpaceDrawRectangle = source.ScreenSpaceDrawQuad.AABBFloat;
            FilteringMode = container.PixelSnapping ? All.Nearest : All.Linear;

            UpdateVersion = container.UpdateVersion;
            BackgroundColour = container.BackgroundColour;

            BlendingParameters localEffectBlending = EffectBlending;
            if (localEffectBlending.Mode == BlendingMode.Inherit)
                localEffectBlending.Mode = source.Blending.Mode;

            if (localEffectBlending.RGBEquation == BlendingEquation.Inherit)
                localEffectBlending.RGBEquation = source.Blending.RGBEquation;

            if (localEffectBlending.AlphaEquation == BlendingEquation.Inherit)
                localEffectBlending.AlphaEquation = source.Blending.AlphaEquation;

            EffectColour = container.EffectColour;
            EffectBlending = localEffectBlending;
            EffectPlacement = container.EffectPlacement;

            DrawOriginal = container.DrawOriginal;
            BlurSigma = container.BlurSigma;
            BlurRadius = new Vector2I(Blur.KernelSize(BlurSigma.X), Blur.KernelSize(BlurSigma.Y));
            BlurRotation = container.BlurRotation;

            Formats.Clear();
            Formats.AddRange(container.AttachedFormats);

            BlurShader = container.BlurShader;
            // BufferedContainer overrides DrawColourInfo for children, but needs to be reset to draw ourselves
            DrawColourInfo = Source.BaseDrawColourInfo;
        }

        public override bool AddChildDrawNodes => RequiresRedraw;

        /// <summary>
        /// Whether this <see cref="BufferedContainerDrawNode"/> should have its children re-drawn.
        /// </summary>
        public bool RequiresRedraw => UpdateVersion > SharedData.DrawVersion;

        private ValueInvokeOnDisposal establishFrameBufferViewport(Vector2 roundedSize)
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

            return new ValueInvokeOnDisposal(returnViewport);
        }

        private void returnViewport()
        {
            GLWrapper.PopViewport();
            GLWrapper.PopMaskingInfo();
        }

        private ValueInvokeOnDisposal bindFrameBuffer(FrameBuffer frameBuffer, Vector2 requestedSize)
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

            return new ValueInvokeOnDisposal(frameBuffer.Unbind);
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

            GLWrapper.SetBlend(new BlendingInfo(BlendingMode.None));

            using (bindFrameBuffer(target, source.Size))
            {
                BlurShader.GetUniform<int>(@"g_Radius").UpdateValue(ref kernelRadius);
                BlurShader.GetUniform<float>(@"g_Sigma").UpdateValue(ref sigma);

                Vector2 size = source.Size;
                BlurShader.GetUniform<Vector2>(@"g_TexSize").UpdateValue(ref size);

                float radians = -MathHelper.DegreesToRadians(blurRotation);
                Vector2 blur = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
                BlurShader.GetUniform<Vector2>(@"g_BlurDirection").UpdateValue(ref blur);

                BlurShader.Bind();
                drawFrameBufferToBackBuffer(source, new RectangleF(0, 0, source.Texture.Width, source.Texture.Height), ColourInfo.SingleColour(Color4.White));
                BlurShader.Unbind();
            }
        }

        private int currentFrameBufferIndex;
        private FrameBuffer currentFrameBuffer => SharedData.FrameBuffers[currentFrameBufferIndex];
        private FrameBuffer advanceFrameBuffer() => SharedData.FrameBuffers[currentFrameBufferIndex = (currentFrameBufferIndex + 1) % 2];

        /// <summary>
        /// Makes sure the first frame buffer is always the one we want to draw from.
        /// This saves us the need to sync the draw indices across draw node trees
        /// since the SharedData.FrameBuffers array is already shared.
        /// </summary>
        private void finalizeFrameBuffer()
        {
            if (currentFrameBufferIndex != 0)
            {
                Trace.Assert(currentFrameBufferIndex == 1,
                    $"Only the first two framebuffers should be the last to be written to at the end of {nameof(Draw)}.");

                FrameBuffer temp = SharedData.FrameBuffers[0];
                SharedData.FrameBuffers[0] = SharedData.FrameBuffers[1];
                SharedData.FrameBuffers[1] = temp;

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
                SharedData.DrawVersion = UpdateVersion;

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
                GLWrapper.SetBlend(DrawColourInfo.Blending);
                drawFrameBufferToBackBuffer(SharedData.FrameBuffers[originalIndex], drawRectangle, DrawColourInfo.Colour);
            }

            // Blit the final framebuffer to screen.
            GLWrapper.SetBlend(new BlendingInfo(EffectBlending));

            ColourInfo effectColour = DrawColourInfo.Colour;
            effectColour.ApplyChild(EffectColour);
            drawFrameBufferToBackBuffer(SharedData.FrameBuffers[0], drawRectangle, effectColour);

            if (DrawOriginal && EffectPlacement == EffectPlacement.Behind)
            {
                GLWrapper.SetBlend(DrawColourInfo.Blending);
                drawFrameBufferToBackBuffer(SharedData.FrameBuffers[originalIndex], drawRectangle, DrawColourInfo.Colour);
            }

            Shader.Unbind();
        }
    }
}
