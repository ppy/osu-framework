// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainer : Container
    {
        private FrameBuffer frameBuffer;
        private QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(1, 3);

        private List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode();
        protected override bool IsCompatibleDrawNode(DrawNode node) => node.GetType() == typeof(BufferedContainerDrawNode);

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferedContainerDrawNode n = node as BufferedContainerDrawNode;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.Batch = quadBatch;
            n.FrameBuffer = frameBuffer;
            n.Formats = new List<RenderbufferInternalFormat>(attachedFormats);

            base.ApplyDrawNode(node);
        }

        public BufferedContainer()
        {
            frameBuffer = new FrameBuffer();
            RelativeSizeAxes = Axes.Both;
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
