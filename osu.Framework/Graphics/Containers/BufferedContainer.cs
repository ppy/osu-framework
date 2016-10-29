// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK.Graphics.ES20;
using osu.Framework.Threading;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainer : Container
    {
        public bool CacheDrawnFrameBuffer = false;
        public Color4 BackgroundColour = new Color4(0, 0, 0, 0);

        private FrameBuffer frameBuffer = new FrameBuffer();

        // If this counter contains a value larger then 0, then we have to redraw.
        private AtomicCounter forceRedraw = new AtomicCounter();
        private QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(1, 3);

        private List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

        protected override bool CanBeFlattened => false;

        public BufferedContainer()
        {
            // The initial draw cannot be cached, and thus we need to initialize
            // with a forced draw.
            ForceRedraw();
        }

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferedContainerDrawNode n = node as BufferedContainerDrawNode;

            n.ScreenSpaceDrawRectangle = ScreenSpaceDrawQuad.AABBf;
            n.Batch = quadBatch;
            n.FrameBuffer = frameBuffer;
            n.Formats = new List<RenderbufferInternalFormat>(attachedFormats);
            n.ForceRedraw = forceRedraw;
            n.BackgroundColour = BackgroundColour;

            base.ApplyDrawNode(node);

            // Our own draw node should contain our correct color, hence we have
            // to undo our overridden DrawInfo getter here.
            n.DrawInfo.Colour = base.DrawInfo.Colour;
        }

        public void Attach(RenderbufferInternalFormat format)
        {
            if (attachedFormats.Exists(f => f == format))
                return;

            attachedFormats.Add(format);
        }

        public void ForceRedraw()
        {
            forceRedraw.Increment();
        }

        protected override void Update()
        {
            // Invalidate drawn frame buffer every frame.
            if (!CacheDrawnFrameBuffer)
                ForceRedraw();

            base.Update();
        }

        protected override DrawInfo DrawInfo
        {
            get
            {
                DrawInfo result = base.DrawInfo;

                // When drawing our children to the frame buffer we do not
                // want their colour to be polluted by their parent (us!)
                // since our own color will be applied on top when we render
                // from the frame buffer to the back buffer later on.
                result.Colour = Color4.White;
                return result;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            frameBuffer.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
