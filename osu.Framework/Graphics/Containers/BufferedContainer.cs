﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using System;
using System.Collections.Generic;

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
    {
    };

    /// <summary>
    /// A container that renders its children to an internal framebuffer, and then
    /// blits the framebuffer to the screen, instead of directly rendering the children
    /// to the screen. This allows otherwise impossible effects to be applied to the
    /// appearance of the container at the cost of performance. Such effects include
    /// uniform fading of children, blur, and other post-processing effects.
    /// </summary>
    public class BufferedContainer<T> : Container<T>, IBufferedContainer
        where T : Drawable
    {
        private bool drawOriginal;

        /// <summary>
        /// If true the original buffered children will be drawn a second time on top of any effect (e.g. blur).
        /// </summary>
        public bool DrawOriginal
        {
            get { return drawOriginal; }

            set
            {
                if (drawOriginal == value)
                    return;

                drawOriginal = value;
                ForceRedraw();
            }
        }


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

        private ColourInfo effectColour = Color4.White;

        /// <summary>
        /// The multiplicative colour of drawn buffered object after applying all effects (e.g. blur). Default is <see cref="Color4.White"/>.
        /// Does not affect the original which is drawn when <see cref="DrawOriginal"/> is true.
        /// </summary>
        public ColourInfo EffectColour
        {
            get { return effectColour; }

            set
            {
                if (effectColour.Equals(value))
                    return;

                effectColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private BlendingParameters effectBlending;

        /// <summary>
        /// The <see cref="BlendingParameters"/> to use after applying all effects. Default is <see cref="BlendingMode.Inherit"/>.
        /// <see cref="BlendingMode.Inherit"/> inherits the blending mode of the original, i.e. <see cref="Drawable.Blending"/> is used.
        /// Does not affect the original which is drawn when <see cref="DrawOriginal"/> is true.
        /// </summary>
        public BlendingParameters EffectBlending
        {
            get { return effectBlending; }

            set
            {
                if (effectBlending.Equals(value))
                    return;

                effectBlending = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private EffectPlacement effectPlacement;

        /// <summary>
        /// Whether the buffered effect should be drawn behind or in front of the original. 
        /// Behind by default. Does not have any effect if <see cref="DrawOriginal"/> is false.
        /// </summary>
        public EffectPlacement EffectPlacement
        {
            get { return effectPlacement; }

            set
            {
                if (effectPlacement == value)
                    return;

                effectPlacement = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4 backgroundColour = new Color4(0, 0, 0, 0);

        /// <summary>
        /// The background colour of the framebuffer. Transparent black by default.
        /// </summary>
        public Color4 BackgroundColour
        {
            get { return backgroundColour; }

            set
            {
                if (backgroundColour == value)
                    return;

                backgroundColour = value;
                ForceRedraw();
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
        /// We need 2 frame buffers such that we can accumulate post-processing effects in a
        /// ping-pong fashion going back and forth (reading from one buffer, writing into the other).
        /// </summary>
        private readonly FrameBuffer[] frameBuffers = new FrameBuffer[3];

        /// <summary>
        /// In order to signal the draw thread to re-draw the buffered container we version it.
        /// Our own version (update) keeps track of which version we are on, whereas the
        /// drawVersion keeps track of the version the draw thread is on.
        /// When forcing a redraw we increment updateVersion, pass it into each new drawnode
        /// and the draw thread will realize its drawVersion is lagging behind, thus redrawing.
        /// </summary>
        private long updateVersion;

        /// <summary>
        /// We also want to keep track of updates to our children, as we can bypass these updates
        /// when our output is in a cached state.
        /// </summary>
        private long childrenUpdateVersion;

        private readonly AtomicCounter drawVersion = new AtomicCounter();

        private readonly QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(1, 3);

        private readonly List<RenderbufferInternalFormat> attachedFormats = new List<RenderbufferInternalFormat>();

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
                blurShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
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
            n.BackgroundColour = backgroundColour;

            BlendingParameters localEffectBlending = EffectBlending;
            if (localEffectBlending.Mode == BlendingMode.Inherit)
                localEffectBlending.Mode = Blending.Mode;

            if (localEffectBlending.RGBEquation == BlendingEquation.Inherit)
                localEffectBlending.RGBEquation = Blending.RGBEquation;

            if (localEffectBlending.AlphaEquation == BlendingEquation.Inherit)
                localEffectBlending.AlphaEquation = Blending.AlphaEquation;

            n.EffectColour = effectColour;
            n.EffectBlending = localEffectBlending;
            n.EffectPlacement = effectPlacement;

            n.DrawOriginal = drawOriginal;
            n.BlurSigma = blurSigma;
            n.BlurRadius = new Vector2I(Blur.KernelSize(BlurSigma.X), Blur.KernelSize(BlurSigma.Y));
            n.BlurRotation = blurRotation;
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

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            childrenUpdateVersion = updateVersion;
        }

        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate && childrenUpdateVersion != updateVersion;

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

        //protected override void Dispose(bool isDisposing)
        //{
        //     right now we are relying on the finalizer for correct disposal.
        //     correct method would be to schedule these to update thread and
        //     then to the draw thread.

        //    foreach (FrameBuffer frameBuffer in frameBuffers)
        //      frameBuffer.Dispose();

        //    base.Dispose(isDisposing);
        //}
    }

    public enum EffectPlacement
    {
        Behind,
        InFront,
    }
}
