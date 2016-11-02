// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK.Graphics.ES30;
using osu.Framework.Threading;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shaders;
using System;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class BufferedContainer : Container
    {
        private Vector2 blurSigma = Vector2.Zero;
        public Vector2 BlurSigma
        {
            get { return blurSigma; }
            set
            {
                if (blurSigma == value)
                    return;

                blurSigma = value;
                ForceRedraw();
            }
        }

        private float blurRotation = 0;
        public float BlurRotation
        {
            get { return blurRotation; }
            set
            {
                if (blurRotation == value)
                    return;

                blurRotation = value;
                ForceRedraw();
            }
        }

        private Shader blurShader;

        public bool CacheDrawnFrameBuffer = false;
        public Color4 BackgroundColour = new Color4(0, 0, 0, 0);

        // We need 2 frame buffers such that we can accumulate post-processing effects in a
        // ping-pong fashion going back and forth (reading from one buffer, writing into the other).
        private FrameBuffer[] frameBuffers = new FrameBuffer[2];

        // If this counter contains a value larger then 0, then we have to redraw.
        private AtomicCounter forceRedraw = new AtomicCounter();
        private QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(1, 3);

        private List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

        protected override bool CanBeFlattened => false;

        public BufferedContainer()
        {
            for (int i = 0; i < frameBuffers.Length; ++i)
                frameBuffers[i] = new FrameBuffer();

            // The initial draw cannot be cached, and thus we need to initialize
            // with a forced draw.
            ForceRedraw();
        }

        protected override void Load(BaseGame game)
        {
            base.Load(game);

            if (blurShader == null)
                blurShader = game?.Shaders?.Load(new ShaderDescriptor(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.Blur));
        }

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferedContainerDrawNode n = node as BufferedContainerDrawNode;

            n.ScreenSpaceDrawRectangle = ScreenSpaceDrawQuad.AABBf;
            n.Batch = quadBatch;
            n.FrameBuffers = frameBuffers;
            n.Formats = new List<RenderbufferInternalFormat>(attachedFormats);

            n.ForceRedraw = forceRedraw;
            n.BackgroundColour = BackgroundColour;

            n.BlurSigma = BlurSigma;
            n.BlurRotation = BlurRotation;
            n.BlurShader = blurShader;

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
            Invalidate(Invalidation.DrawNode);
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
            foreach (FrameBuffer frameBuffer in frameBuffers)
                frameBuffer.Dispose();

            base.Dispose(isDisposing);
        }
    }
}
