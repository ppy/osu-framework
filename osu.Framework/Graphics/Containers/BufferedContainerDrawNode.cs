// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK;
using OpenTK.Graphics.ES20;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNode : ContainerDrawNode
    {
        public FrameBuffer FrameBuffer;
        public Rectangle DrawRectangle;
        public QuadBatch<TexturedVertex2D> Batch;
        public List<RenderbufferInternalFormat> Formats;

        protected override void Draw()
        {
            if (!FrameBuffer.IsInitialized)
                FrameBuffer.Initialize();

            foreach (var f in Formats)
                FrameBuffer.Attach(f);

            FrameBuffer.Size = new Vector2(DrawRectangle.Width, DrawRectangle.Height);

            FrameBuffer.Bind();

            // Set viewport to the texture size
            GLWrapper.PushViewport(new Rectangle(0, 0, DrawRectangle.Width, DrawRectangle.Height));
            // We need to draw children as if they were zero-based to the top-left of the texture
            // so we make the new zero be this container's position without affecting children in any negative ways
            GLWrapper.PushOrtho(new Rectangle(DrawRectangle.X, DrawRectangle.Y, DrawRectangle.Width, DrawRectangle.Height));

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // The actual Draw call
            base.Draw();

            GLWrapper.PopOrtho();
            GLWrapper.PopViewport();

            FrameBuffer.Unbind();

            GLWrapper.SetBlend(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Shader.Bind();

            Rectangle textureRect = new Rectangle(0, 0, DrawRectangle.Width, DrawRectangle.Height);
            FrameBuffer.Texture.Draw(new Quad(DrawRectangle.X, DrawRectangle.Y, DrawRectangle.Width, DrawRectangle.Height), textureRect, DrawInfo.Colour, null);

            Shader.Unbind();

            // In the case of nested framebuffer containerse we need to draw to
            // the last framebuffer container immediately, so let's force it
            Batch.Draw();
        }
    }
}
