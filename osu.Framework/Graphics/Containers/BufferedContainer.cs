// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK.Graphics.ES30;
using osu.Framework.Threading;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shaders;
using OpenTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Transformations;
using System;

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

        private float blurRotation;
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

        private bool pixelSnapping;
        public bool PixelSnapping
        {
            get { return pixelSnapping; }
            set
            {
                if (frameBuffers[0].IsInitialized || frameBuffers[1].IsInitialized)
                    throw new InvalidOperationException("May only set PixelSnapping before FrameBuffers are initialized (i.e. before the first draw).");
                pixelSnapping = value;
            }
        }
        private Shader blurShader;

        public bool CacheDrawnFrameBuffer;
        public Color4 BackgroundColour = new Color4(0, 0, 0, 0);

        // We need 2 frame buffers such that we can accumulate post-processing effects in a
        // ping-pong fashion going back and forth (reading from one buffer, writing into the other).
        private FrameBuffer[] frameBuffers = new FrameBuffer[2];

        // In order to signal the draw thread to re-draw the buffered container we version it.
        // Our own version (update) keeps track of which version we are on, whereas the
        // drawVersion keeps track of the version the draw thread is on.
        // When forcing a redraw we increment updateVersion, pass it into each new drawnode
        // and the draw thread will realize its drawVersion is lagging behind, thus redrawing.
        private long updateVersion;
        private AtomicCounter drawVersion = new AtomicCounter();

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

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            if (blurShader == null)
                blurShader = shaders?.Load(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.Blur);
        }

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferedContainerDrawNode n = (BufferedContainerDrawNode)node;

            n.ScreenSpaceDrawRectangle = ScreenSpaceDrawQuad.AABBFloat;
            n.Batch = quadBatch;
            n.FrameBuffers = frameBuffers;
            n.Formats = new List<RenderbufferInternalFormat>(attachedFormats);
            n.FilteringMode = pixelSnapping ? All.Nearest : All.Linear;

            n.DrawVersion = drawVersion;
            n.UpdateVersion = updateVersion;
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
            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        protected override void Update()
        {
            // Invalidate drawn frame buffer every frame.
            if (!CacheDrawnFrameBuffer)
                ForceRedraw();

            base.Update();
        }

        public override DrawInfo DrawInfo
        {
            get
            {
                DrawInfo result = base.DrawInfo;

                // When drawing our children to the frame buffer we do not
                // want their colour to be polluted by their parent (us!)
                // since our own color will be applied on top when we render
                // from the frame buffer to the back buffer later on.
                result.Colour = ColourInfo.SingleColour(Color4.White);
                return result;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            // right now we are relying on the finalizer for correct disposal.
            // correct method would be to schedule these to update thread and
            // then to the draw thread.

            //foreach (FrameBuffer frameBuffer in frameBuffers)
            //  frameBuffer.Dispose();

            base.Dispose(isDisposing);
        }

        public void BlurTo(Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformBlurSigma));
            TransformVectorTo(BlurSigma, newBlurSigma, duration, easing, new TransformBlurSigma());
        }

        protected class TransformBlurSigma : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);
                BufferedContainer bufferedContainer = (BufferedContainer)d;
                bufferedContainer.BlurSigma = CurrentValue;
            }
        }
    }
}
