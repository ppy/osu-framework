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
        public Vector2 BlurSigma
        {
            get => blurSigma;
            set
            {
                if (value == blurSigma)
                    return;

                blurSigma = value;
                updateRefCount();
            }
        }

        private Vector2 blurSigma;

        public float BlurRotation { get; set; }

        public virtual float BackdropOpacity => MathF.Min(1, (FrameBufferDrawColour?.Colour.MaxAlpha ?? 1) * 2.5f);

        public float MaskCutoff { get; set; }

        public float BackdropTintStrength { get; set; }

        public Vector2 EffectBufferScale { get; set; } = Vector2.One;

        [Resolved]
        private IBackbufferProvider backbufferProvider { get; set; }

        protected override BufferedDrawNodeSharedData CreateSharedData() => new BackdropBlurDrawNodeSharedData(new[] { RenderBufferFormat.D16 });

        protected override DrawNode CreateDrawNode() => new BackdropBlurDrawNode(this, new PathDrawNode(this), (BackdropBlurDrawNodeSharedData)SharedData);

        IShader IBackdropBlurDrawable.BlurShader => blurShader;

        IShader IBackdropBlurDrawable.BlendShader => blendShader;

        private IShader blurShader = null!;

        private IShader blendShader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            blurShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BLUR);
            blendShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.BACKDROP_BLUR_BLEND);
        }

        private RectangleF lastBackBufferDrawRect;

        RectangleF IBackdropBlurDrawable.LastBackBufferDrawRect => lastBackBufferDrawRect;

        protected override void Update()
        {
            base.Update();

            lastBackBufferDrawRect = backbufferProvider.ScreenSpaceDrawQuad.AABBFloat;
        }

        private bool isRefCounted;

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            if ((invalidation & Invalidation.Parent) > 0)
                updateRefCount();

            return base.OnInvalidate(invalidation, source);
        }

        private void updateRefCount()
        {
            if (backbufferProvider is RefCountedBackbufferProvider refCount)
            {
                bool shouldBeRefCounted = Parent != null && (BlurSigma.X > 0 || BlurSigma.Y > 0);

                if (shouldBeRefCounted != isRefCounted)
                {
                    if (shouldBeRefCounted)
                        refCount.Increment();
                    else
                        refCount.Decrement();

                    isRefCounted = shouldBeRefCounted;
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isRefCounted && backbufferProvider is RefCountedBackbufferProvider refCount)
            {
                refCount.Decrement();
                isRefCounted = false;
            }

            base.Dispose(isDisposing);
        }
    }
}
