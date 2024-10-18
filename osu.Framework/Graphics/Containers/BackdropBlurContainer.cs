// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Layout;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that blurs the content of its nearest parent <see cref="IBufferedContainer"/> behind its children.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="BackdropBlurContainer{T}"/>.
    /// </summary>
    public partial class BackdropBlurContainer : BackdropBlurContainer<Drawable>
    {
    }

    /// <summary>
    /// A container that blurs the content of its nearest parent <see cref="IBufferedContainer"/> behind its children.
    /// </summary>
    public partial class BackdropBlurContainer<T> : Container<T>, IBufferedContainer, IBufferedDrawable where T : Drawable
    {
        [Resolved]
        private IBackbufferProvider backbufferProvider { get; set; } = null!;

        /// <summary>
        /// Controls the amount of blurring in two orthogonal directions (X and Y if
        /// <see cref="BlurRotation"/> is zero).
        /// Blur is parametrized by a gaussian image filter. This property controls
        /// the standard deviation (sigma) of the gaussian kernel.
        /// </summary>
        public Vector2 BlurSigma { get; set; } = Vector2.Zero;

        /// <summary>
        /// Rotates the blur kernel clockwise. In degrees. Has no effect if
        /// <see cref="BlurSigma"/> has the same magnitude in both directions.
        /// </summary>
        public float BlurRotation;

        /// <summary>
        /// The multiplicative colour of drawn buffered object after applying all effects (e.g. blur). Default is <see cref="Color4.White"/>.
        /// </summary>
        public ColourInfo EffectColour = Color4.White;

        /// <summary>
        /// The alpha at which the content is no longer considered opaque & the background will not be blurred behind it.
        /// </summary>
        public float MaskCutoff;

        public IShader TextureShader { get; private set; } = null!;

        private IShader blurShader = null!;

        private IShader textureMaskShader = null!;

        private readonly BackdropBlurContainerDrawNodeSharedData sharedData;

        private readonly LayoutValue parentBacking = new LayoutValue(Invalidation.Parent);

        public BackdropBlurContainer(RenderBufferFormat[]? formats = null, bool pixelSnapping = false)
        {
            sharedData = new BackdropBlurContainerDrawNodeSharedData(formats, pixelSnapping);

            AddLayout(parentBacking);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            textureMaskShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_MASK);
        }

        protected override DrawNode CreateDrawNode() => new BackdropBlurDrawNode(this, sharedData);

        private RectangleF lastParentDrawRect;

        protected override void Update()
        {
            base.Update();

            Invalidate(Invalidation.DrawNode);

            lastParentDrawRect = backbufferProvider.ScreenSpaceDrawQuad.AABBFloat;
        }

        private bool hadParent;

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            if ((invalidation & Invalidation.Parent) > 0 && backbufferProvider is RefCountedBackbufferProvider refCount)
            {
                bool hasParent = Parent != null;

                if (hasParent != hadParent)
                {
                    if (hasParent)
                        refCount.Increment();
                    else
                        refCount.Decrement();

                    hadParent = hasParent;
                }
            }

            return base.OnInvalidate(invalidation, source);
        }

        public Color4 BackgroundColour => Color4.Transparent;
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

        public Vector2 FrameBufferScale { get; set; } = Vector2.One;

        public Vector2 EffectBufferScale { get; set; } = Vector2.One;

        protected override void Dispose(bool isDisposing)
        {
            if (hadParent && backbufferProvider is RefCountedBackbufferProvider refCount)
            {
                refCount.Decrement();
                hadParent = false;
            }

            base.Dispose(isDisposing);
        }
    }
}
