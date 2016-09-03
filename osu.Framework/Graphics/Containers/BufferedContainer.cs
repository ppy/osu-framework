//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics.ES20;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using osu.Framework.Graphics.Batches;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainer : LargeContainer
    {
        private FrameBuffer frameBuffer;
        private QuadBatch<TexturedVertex2d> quadBatch = new QuadBatch<TexturedVertex2d>(1, 3);

        private List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

        protected override DrawNode BaseDrawNode => new BufferedContainerDrawNode(DrawInfo, frameBuffer, ScreenSpaceDrawQuad, quadBatch, attachedFormats);

        public BufferedContainer()
        {
            frameBuffer = new FrameBuffer();
        }

        public void Attach(RenderbufferInternalFormat format)
        {
            if (attachedFormats.Exists(f => f == format))
                return;

            attachedFormats.Add(format);
        }

        protected override void Dispose(bool isDisposing)
        {
            frameBuffer.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
