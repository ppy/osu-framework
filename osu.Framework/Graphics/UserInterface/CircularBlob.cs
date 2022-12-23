// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class CircularBlob : Sprite
    {
        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, IRenderer renderer)
        {
            Texture ??= renderer.WhitePixel;
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "CircularBlob");
        }

        protected override DrawNode CreateDrawNode() => new CircularBlobDrawNode(this);

        private float innerRadius = 0.2f;

        /// <summary>
        /// The inner fill radius, relative to the <see cref="Drawable.DrawSize"/> of the <see cref="CircularBlob"/>.
        /// The value range is 0 to 1 where 0 is invisible and 1 is completely filled.
        /// The entire texture still fills the disk without cropping it.
        /// </summary>
        public float InnerRadius
        {
            get => innerRadius;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(InnerRadius)} must be finite, but is {value}.");

                innerRadius = Math.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float amplitude = 0.3f;

        public float Amplitude
        {
            get => amplitude;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(Amplitude)} must be finite, but is {value}.");

                amplitude = Math.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        }

        private int pointCount = 12;

        public int PointCount
        {
            get => pointCount;
            set
            {
                pointCount = Math.Max(value, 3);
                Invalidate(Invalidation.DrawNode);
            }
        }

        private int seed = 1;

        public int Seed
        {
            get => seed;
            set
            {
                seed = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private class CircularBlobDrawNode : SpriteDrawNode
        {
            public new CircularBlob Source => (CircularBlob)base.Source;

            public CircularBlobDrawNode(CircularBlob source)
                : base(source)
            {
            }

            private float pathRadius;
            private float texelSize;
            private int pointCount;
            private float amplitude;
            private Vector2 noisePosition;
            private int seed = -1;

            public override void ApplyState()
            {
                base.ApplyState();

                pathRadius = Source.innerRadius * 0.5f;
                pointCount = Source.pointCount;
                amplitude = Source.amplitude;

                int newSeed = Source.seed;

                if (seed != newSeed)
                {
                    Random rand = new Random(newSeed);
                    noisePosition = new Vector2((float)(rand.NextDouble() * 1000), (float)(rand.NextDouble() * 1000));
                    seed = newSeed;
                }

                // smoothstep looks too sharp with 1px, let's give it a bit more
                texelSize = 2.0f / Math.Min(ScreenSpaceDrawQuad.Width, ScreenSpaceDrawQuad.Height);
            }

            protected override void Blit(IRenderer renderer)
            {
                if (pathRadius == 0)
                    return;

                var shader = TextureShader;

                shader.GetUniform<float>("pathRadius").UpdateValue(ref pathRadius);
                shader.GetUniform<float>("texelSize").UpdateValue(ref texelSize);
                shader.GetUniform<int>("pointCount").UpdateValue(ref pointCount);
                shader.GetUniform<float>("amplitude").UpdateValue(ref amplitude);
                shader.GetUniform<Vector2>("noisePosition").UpdateValue(ref noisePosition);

                base.Blit(renderer);
            }

            protected internal override bool CanDrawOpaqueInterior => false;
        }
    }
}
