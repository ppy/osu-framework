﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
using osu.Framework.Graphics.Transforms;
using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that renders its children to an internal framebuffer, and then
    /// blits the framebuffer to the screen, instead of directly rendering the children
    /// to the screen. This allows otherwise impossible effects to be applied to the
    /// appearance of the container at the cost of performance. Such effects include
    /// uniform fading of children, blur, and other post-processing effects.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="BufferedContainer{T}"/>.
    /// </summary>
    public class BufferedContainer : BufferedContainer<Drawable>
    { };

    /// <summary>
    /// A container that renders its children to an internal framebuffer, and then
    /// blits the framebuffer to the screen, instead of directly rendering the children
    /// to the screen. This allows otherwise impossible effects to be applied to the
    /// appearance of the container at the cost of performance. Such effects include
    /// uniform fading of children, blur, and other post-processing effects.
    /// </summary>
    public class BufferedContainer<T> : Container<T> where T : Drawable
    {
        private Vector2 blurSigma = Vector2.Zero;

        /// <summary>
        /// Controls the amount of blurring in two orthogonal directions (X and Y if 
        /// <see cref="BlurRotation"/> is zero).
        /// Blur is parametrized by a gaussian image filter. This property controls
        /// the standard deviation (sigma) of the gaussian kernel.
        /// </summary>
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

        /// <summary>
        /// Rotates the blur kernel clockwise. In degrees. Has no effect if
        /// <see cref="BlurSigma"/> has the same magnitude in both directions.
        /// </summary>
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

        /// <summary>
        /// Whether the framebuffer's position is snapped to the nearest pixel when blitting.
        /// Since the framebuffer's texels have the same size as pixels, this amounts to setting
        /// the texture filtering mode to "nearest".
        /// </summary>
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

        /// <summary>
        /// Whether the rendered framebuffer shall be cached until <see cref="ForceRedraw"/> is called
        /// or the size of the container (i.e. framebuffer) changes.
        /// If false, then the framebuffer is re-rendered before it is blitted to the screen; equivalent
        /// to calling <see cref="ForceRedraw"/> every frame.
        /// </summary>
        public bool CacheDrawnFrameBuffer;

        /// <summary>
        /// Forces a redraw of the framebuffer before it is blitted the next time.
        /// Only relevant if <see cref="CacheDrawnFrameBuffer"/> is true.
        /// </summary>
        public void ForceRedraw()
        {
            ++updateVersion;
            Invalidate(Invalidation.DrawNode);
        }

        /// <summary>
        /// The background colour of the framebuffer. Transparent black by default.
        /// </summary>
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

        /// <summary>
        /// Constructs an empty buffered container.
        /// </summary>
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

        /// <summary>
        /// Attach an additional component to the framebuffer. Such a component can e.g.
        /// be a depth component, such that the framebuffer can hold fragment depth information.
        /// </summary>
        public void Attach(RenderbufferInternalFormat format)
        {
            if (attachedFormats.Exists(f => f == format))
                return;

            attachedFormats.Add(format);
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

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{T}"/> that blurs
        /// the buffered container.
        /// </summary>
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
