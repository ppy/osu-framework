// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using Rectangle = System.Drawing.Rectangle;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK;
using OpenTK.Graphics.ES20;
using OpenTK.Graphics;
using osu.Framework.Threading;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNode : ContainerDrawNode
    {
        public FrameBuffer FrameBuffer;

        // If this counter contains a value larger then 0, then we have to redraw.
        public AtomicCounter ForceRedraw;

        public RectangleF ScreenSpaceDrawRectangle;
        public QuadBatch<TexturedVertex2D> Batch;
        public List<RenderbufferInternalFormat> Formats;

        protected void DrawToFrameBuffer()
        {
            if (!FrameBuffer.IsInitialized)
                FrameBuffer.Initialize();

            // These additional render buffers are only required if e.g. depth
            // or stencil information needs to also be stored somewhere.
            foreach (var f in Formats)
                FrameBuffer.Attach(f);

            // This setter will also take care of allocating a texture of appropriate size within the framebuffer.
            FrameBuffer.Size = new Vector2(ScreenSpaceDrawRectangle.Width, ScreenSpaceDrawRectangle.Height);

            FrameBuffer.Bind();

            // Disable masking for generating the frame buffer since masking will be re-applied
            // when actually drawing later on anyways. This allows more information to be captured
            // in the frame buffer and helps with cached buffers being re-used.
            GLWrapper.PushScissor(new MaskingInfo
            {
                ScreenSpaceAABB = ScreenSpaceDrawRectangle.AABB,
                MaskingRect = ScreenSpaceDrawRectangle,
                ToMaskingSpace = Matrix3.Identity,
            }, true);

            // Match viewport to FrameBuffer such that we don't draw unnecessary pixels.
            GLWrapper.PushViewport(new Rectangle(0, 0, FrameBuffer.Texture.Width, FrameBuffer.Texture.Height));

            // We need to draw children as if they were zero-based to the top-left of the texture.
            // We can do this by adding a translation component to our (orthogonal) projection matrix.
            GLWrapper.PushOrtho(ScreenSpaceDrawRectangle);

            // The actual drawing of children.
            GLWrapper.ClearColour(Color4.Transparent);
            base.Draw();

            GLWrapper.PopOrtho();
            GLWrapper.PopViewport();

            GLWrapper.PopScissor();

            FrameBuffer.Unbind();
        }

        protected void DrawToScreen()
        {
            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Shader.Bind();

            // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
            Primitives.RectangleF textureRect = new Primitives.RectangleF(0, FrameBuffer.Texture.Height, FrameBuffer.Texture.Width, -FrameBuffer.Texture.Height);
            if (FrameBuffer.Texture.Bind())
                // Color was already applied by base.Draw(); no need to re-apply. Thus we use White here.
                FrameBuffer.Texture.Draw(ScreenSpaceDrawRectangle, textureRect, DrawInfo.Colour);

            Shader.Unbind();
        }

        protected override void Draw()
        {
            if (ForceRedraw.Reset() > 0)
                DrawToFrameBuffer();
            DrawToScreen();
        }
    }
}
