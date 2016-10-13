// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNode : DrawNode
    {
        public FrameBuffer FrameBuffer;
        public Quad ScreenSpaceDrawQuad;
        public QuadBatch<TexturedVertex2D> Batch;
        public List<RenderbufferInternalFormat> Formats;

        protected override void PreDraw()
        {
            base.PreDraw();

            foreach (var f in Formats)
                FrameBuffer.Attach(f);

            FrameBuffer.Size = new Vector2(ScreenSpaceDrawQuad.Width, ScreenSpaceDrawQuad.Height);

            FrameBuffer.Bind();

            // Set viewport to the texture size
            GLWrapper.PushViewport(new Rectangle(0, 0, FrameBuffer.Texture.Width, FrameBuffer.Texture.Height));
            // We need to draw children as if they were zero-based to the top-left of the texture
            // so we make the new zero be this container's position without affecting children in any negative ways
            GLWrapper.PushOrtho(new Rectangle((int)ScreenSpaceDrawQuad.TopLeft.X, (int)ScreenSpaceDrawQuad.TopLeft.Y, FrameBuffer.Texture.Width, FrameBuffer.Texture.Height));

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        protected override void PostDraw()
        {
            base.PostDraw();

            FrameBuffer.Unbind();

            GLWrapper.PopOrtho();
            GLWrapper.PopViewport();

            GLWrapper.SetBlend(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Rectangle textureRect = new Rectangle(0, FrameBuffer.Texture.Height, FrameBuffer.Texture.Width, -FrameBuffer.Texture.Height);
            FrameBuffer.Texture.Draw(ScreenSpaceDrawQuad, textureRect, DrawInfo.Colour, Batch);

            // In the case of nested framebuffer containerse we need to draw to
            // the last framebuffer container immediately, so let's force it
            Batch.Draw();
        }
    }
}
