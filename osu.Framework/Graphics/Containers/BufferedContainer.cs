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
        private bool cacheDrawnFrameBuffer = false;
        public bool CacheDrawnFrameBuffer
        {
            get { return cacheDrawnFrameBuffer; }
            set
            {
                if (cacheDrawnFrameBuffer == value)
                    return;

                cacheDrawnFrameBuffer = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        // We set this to true to make sure we draw at least once.
        private bool resetNextDraw = true;
        public void ResetFrameBuffer()
        {
            resetNextDraw = true;
            Invalidate(Invalidation.DrawNode);
        }

        private FrameBuffer frameBuffer = new FrameBuffer();
        private QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(1, 3);

        private List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

        protected override bool CanBeFlattened => false;

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferedContainerDrawNode n = node as BufferedContainerDrawNode;

            n.DrawRectangle = ScreenSpaceDrawQuad.AABBf;
            n.Batch = quadBatch;
            n.FrameBuffer = frameBuffer;
            n.Formats = new List<RenderbufferInternalFormat>(attachedFormats);

            base.ApplyDrawNode(node);

            if (resetNextDraw)
            {
                resetNextDraw = false;
                n.ShallDraw = true;

                // This invalidation makes sure we will again call ApplyDrawNode
                // before the next draw, then setting n.ShallDraw potentially to false.
                Invalidate(Invalidation.DrawNode);
            }
            else
                n.ShallDraw = !CacheDrawnFrameBuffer;
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
