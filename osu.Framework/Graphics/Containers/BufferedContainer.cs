// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osuTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Utils;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Layout;

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
    public partial class BufferedContainer : BufferedContainer<Drawable>
    {
        /// <inheritdoc />
        public BufferedContainer(RenderBufferFormat[] formats = null, bool pixelSnapping = false, bool cachedFrameBuffer = false)
            : base(formats, pixelSnapping, cachedFrameBuffer)
        {
        }
    }

    /// <summary>
    /// A container that renders its children to an internal framebuffer, and then
    /// blits the framebuffer to the screen, instead of directly rendering the children
    /// to the screen. This allows otherwise impossible effects to be applied to the
    /// appearance of the container at the cost of performance. Such effects include
    /// uniform fading of children, blur, and other post-processing effects.
    /// </summary>
    public partial class BufferedContainer<T> : Container<T>, IBufferedContainer, IBufferedDrawable, IBackbufferProvider
        where T : Drawable
    {
        private bool drawOriginal;

        /// <summary>
        /// If true the original buffered children will be drawn a second time on top of any effect (e.g. blur).
        /// </summary>
        public bool DrawOriginal
        {
            get => drawOriginal;
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
            get => blurSigma;
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
            get => blurRotation;
            set
            {
                if (blurRotation == value)
                    return;

                blurRotation = value;
                ForceRedraw();
            }
        }

        private ColourInfo effectColour = Color4.White;

        /// <summary>
        /// The multiplicative colour of drawn buffered object after applying all effects (e.g. blur). Default is <see cref="Color4.White"/>.
        /// Does not affect the original which is drawn when <see cref="DrawOriginal"/> is true.
        /// </summary>
        public ColourInfo EffectColour
        {
            get => effectColour;
            set
            {
                if (effectColour.Equals(value))
                    return;

                effectColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private BlendingParameters effectBlending = BlendingParameters.Inherit;

        /// <summary>
        /// The <see cref="BlendingParameters"/> to use after applying all effects. Default is <see cref="BlendingType.Inherit"/>.
        /// <see cref="BlendingType.Inherit"/> inherits the blending mode of the original, i.e. <see cref="Drawable.Blending"/> is used.
        /// Does not affect the original which is drawn when <see cref="DrawOriginal"/> is true.
        /// </summary>
        public BlendingParameters EffectBlending
        {
            get => effectBlending;
            set
            {
                if (effectBlending == value)
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
            get => effectPlacement;
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
            get => backgroundColour;
            set
            {
                if (backgroundColour == value)
                    return;

                backgroundColour = value;
                ForceRedraw();
            }
        }

        private Vector2 frameBufferScale = Vector2.One;

        public Vector2 FrameBufferScale
        {
            get => frameBufferScale;
            set
            {
                if (frameBufferScale == value)
                    return;

                frameBufferScale = value;
                ForceRedraw();
            }
        }

        private float grayscaleStrength;

        public float GrayscaleStrength
        {
            get => grayscaleStrength;
            set
            {
                if (grayscaleStrength == value)
                    return;

                grayscaleStrength = value;
                ForceRedraw();
            }
        }

        /// <summary>
        /// Whether the rendered framebuffer is being cached until <see cref="ForceRedraw"/> is called
        /// or the size of the container (i.e. framebuffer) changes.
        /// If false, then the framebuffer is re-rendered before it is blitted to the screen; equivalent
        /// to calling <see cref="ForceRedraw"/> every frame.
        /// </summary>
        public readonly bool UsingCachedFrameBuffer;

        private bool redrawOnScale = true;

        /// <summary>
        /// Whether to redraw this <see cref="BufferedContainer"/> when the draw scale changes.
        /// </summary>
        public bool RedrawOnScale
        {
            get => redrawOnScale;
            set
            {
                if (redrawOnScale == value)
                    return;

                redrawOnScale = value;
                screenSpaceSizeBacking?.Invalidate();
            }
        }

        /// <summary>
        /// Forces a redraw of the framebuffer before it is blitted the next time.
        /// Only relevant if <see cref="UsingCachedFrameBuffer"/> is true.
        /// </summary>
        public void ForceRedraw() => Invalidate(Invalidation.DrawNode);

        /// <summary>
        /// In order to signal the draw thread to re-draw the buffered container we version it.
        /// Our own version (update) keeps track of which version we are on, whereas the
        /// drawVersion keeps track of the version the draw thread is on.
        /// When forcing a redraw we increment updateVersion, pass it into each new drawnode
        /// and the draw thread will realize its drawVersion is lagging behind, thus redrawing.
        /// </summary>
        private long updateVersion;

        public IShader TextureShader { get; private set; }

        private IShader blurShader;
        private IShader grayscaleShader;

        private readonly BufferedContainerDrawNodeSharedData sharedData;

        /// <summary>
        /// Constructs an empty buffered container.
        /// </summary>
        /// <param name="formats">The render buffer formats attached to the frame buffer of this <see cref="BufferedContainer"/>.</param>
        /// <param name="pixelSnapping">
        /// Whether the frame buffer position should be snapped to the nearest pixel when blitting.
        /// This amounts to setting the texture filtering mode to "nearest".
        /// </param>
        /// <param name="cachedFrameBuffer">
        /// Whether the rendered frame buffer should be cached until <see cref="ForceRedraw"/> is called
        /// or the size of the container (i.e. frame buffer) changes.
        /// When disabled, drawing will be clipped to the game window bounds. Enabling can allow drawing larger than (or outside) the game window bounds.
        /// </param>
        public BufferedContainer(RenderBufferFormat[] formats = null, bool pixelSnapping = false, bool cachedFrameBuffer = false)
        {
            UsingCachedFrameBuffer = cachedFrameBuffer;

            sharedData = new BufferedContainerDrawNodeSharedData(formats, pixelSnapping, !cachedFrameBuffer);

            AddLayout(screenSpaceSizeBacking);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            grayscaleShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.GRAYSCALE);
        }

        protected override DrawNode CreateDrawNode() => new BufferedContainerDrawNode(this, sharedData);

        public override bool UpdateSubTreeMasking()
        {
            bool result = base.UpdateSubTreeMasking();

            childrenUpdateVersion = updateVersion;

            return result;
        }

        protected override RectangleF ComputeChildMaskingBounds() => ScreenSpaceDrawQuad.AABBFloat; // Make sure children never get masked away

        private Vector2 lastScreenSpaceSize;

        // We actually only care about Invalidation.MiscGeometry | Invalidation.DrawInfo
        private readonly LayoutValue screenSpaceSizeBacking = new LayoutValue(Invalidation.Presence | Invalidation.RequiredParentSizeToFit | Invalidation.DrawInfo);

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool result = base.OnInvalidate(invalidation, source);

            if ((invalidation & Invalidation.DrawNode) > 0)
            {
                ++updateVersion;
                result = true;
            }

            return result;
        }

        private long childrenUpdateVersion = -1;
        protected override bool RequiresChildrenUpdate => base.RequiresChildrenUpdate && childrenUpdateVersion != updateVersion;

        protected override void Update()
        {
            base.Update();

            // Invalidate drawn frame buffer every frame.
            if (!UsingCachedFrameBuffer)
                ForceRedraw();
            else if (!screenSpaceSizeBacking.IsValid)
            {
                Vector2 drawSize = ScreenSpaceDrawQuad.AABBFloat.Size;

                if (!RedrawOnScale)
                {
                    Matrix3 scaleMatrix = Matrix3.CreateScale(DrawInfo.MatrixInverse.ExtractScale());
                    Vector2Extensions.Transform(ref drawSize, ref scaleMatrix, out drawSize);
                }

                if (!Precision.AlmostEquals(lastScreenSpaceSize, drawSize))
                {
                    ++updateVersion;
                    lastScreenSpaceSize = drawSize;
                }

                screenSpaceSizeBacking.Validate();
            }
        }

        /// <summary>
        /// The blending which <see cref="BufferedContainerDrawNode"/> uses for the effect.
        /// </summary>
        public BlendingParameters DrawEffectBlending
        {
            get
            {
                BlendingParameters blending = EffectBlending;

                blending.CopyFromParent(Blending);
                blending.ApplyDefaultToInherited();

                return blending;
            }
        }

        /// <summary>
        /// Creates a view which can be added to a container to display the content of this <see cref="BufferedContainer{T}"/>.
        /// </summary>
        /// <returns>The view.</returns>
        public BufferedContainerView<T> CreateView() => new BufferedContainerView<T>(this, sharedData);

        public DrawColourInfo? FrameBufferDrawColour => base.DrawColourInfo;

        // Children should not receive the true colour to avoid colour doubling when the frame-buffers are rendered to the back-buffer.
        public override DrawColourInfo DrawColourInfo
        {
            get
            {
                // Todo: This is incorrect.
                var blending = Blending;
                blending.ApplyDefaultToInherited();

                return new DrawColourInfo(Color4.White, blending);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            sharedData.Dispose();
        }
    }

    public enum EffectPlacement
    {
        Behind,
        InFront,
    }
}
