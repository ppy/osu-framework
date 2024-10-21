// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Layout;
using osuTK;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// A <see cref="SmoothPath"/> which can blur the content underneath it.
    /// </summary>
    public partial class BackdropBlurPath : SmoothPath, IBackdropBlurDrawable
    {
        public Vector2 BlurSigma { get; set; } = Vector2.Zero;

        public float BlurRotation { get; set; }

        public virtual float BackdropOpacity => 1 - MathF.Pow(1 - base.DrawColourInfo.Colour.MaxAlpha, 2);

        public float MaskCutoff { get; set; }

        public float BackdropTintStrength { get; set; }

        public Vector2 EffectBufferScale { get; set; } = Vector2.One;

        [Resolved]
        private IBackbufferProvider backbufferProvider { get; set; }

        protected override BufferedDrawNodeSharedData CreateSharedData() => new BufferedDrawNodeSharedData(1, new[] { RenderBufferFormat.D16 }, clipToRootNode: true);

        protected override DrawNode CreateDrawNode() => new BackdropBlurDrawNode(this, new PathDrawNode(this), SharedData);

        IShader IBackdropBlurDrawable.BlurShader => blurShader;

        IShader IBackdropBlurDrawable.BackdropBlurShader => backdropBlurShader;

        private IShader blurShader = null!;

        private IShader backdropBlurShader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            backdropBlurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BACKDROP_BLUR);
        }

        private RectangleF lastBackBufferDrawRect;

        RectangleF IBackdropBlurDrawable.LastBackBufferDrawRect => lastBackBufferDrawRect;

        protected override void Update()
        {
            base.Update();

            lastBackBufferDrawRect = backbufferProvider.ScreenSpaceDrawQuad.AABBFloat;
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
    }
}
