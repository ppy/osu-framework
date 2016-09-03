//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics.ES20;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainerDrawNode : DrawNode
    {
        private FrameBuffer frameBuffer;
        private Quad screenSpaceDrawQuad;
        private QuadBatch<TexturedVertex2d> batch;
        private List<RenderbufferInternalFormat> formats;

        public BufferedContainerDrawNode(DrawInfo drawInfo, FrameBuffer frameBuffer, Quad screenSpaceDrawQuad, QuadBatch<TexturedVertex2d> batch, List<RenderbufferInternalFormat> formats)
            : base(drawInfo)
        {
            this.frameBuffer = frameBuffer;
            this.screenSpaceDrawQuad = screenSpaceDrawQuad;
            this.batch = batch;
            this.formats = new List<RenderbufferInternalFormat>(formats);
        }

        protected override void PreDraw()
        {
            base.PreDraw();

            foreach (var f in formats)
                frameBuffer.Attach(f);

            frameBuffer.Size = new Vector2(screenSpaceDrawQuad.Width, screenSpaceDrawQuad.Height);

            frameBuffer.Bind();

            // Set viewport to the texture size
            GLWrapper.PushViewport(new Rectangle(0, 0, frameBuffer.Texture.Width, frameBuffer.Texture.Height));
            // We need to draw children as if they were zero-based to the top-left of the texture
            // so we make the new zero be this container's position without affecting children in any negative ways
            GLWrapper.PushOrtho(new Rectangle((int)screenSpaceDrawQuad.TopLeft.X, (int)screenSpaceDrawQuad.TopLeft.Y, frameBuffer.Texture.Width, frameBuffer.Texture.Height));

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        protected override void PostDraw()
        {
            base.PostDraw();

            frameBuffer.Unbind();

            GLWrapper.PopOrtho();
            GLWrapper.PopViewport();

            GLWrapper.SetBlend(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Rectangle textureRect = new Rectangle(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);
            frameBuffer.Texture.Draw(screenSpaceDrawQuad, textureRect, DrawInfo.Colour, batch);

            // In the case of nested framebuffer containerse we need to draw to
            // the last framebuffer container immediately, so let's force it
            batch.Draw();
        }
    }
}
