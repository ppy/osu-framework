// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
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

        /// <summary>
        /// The inner fill radius, relative to the <see cref="Drawable.DrawSize"/> of the <see cref="CircularBlob"/>.
        /// The value range is 0 to 1 where 0 is invisible and 1 is completely filled.
        /// The entire texture still fills the disk without cropping it.
        /// </summary>
        public float InnerRadius
        {
            get;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(InnerRadius)} must be finite, but is {value}.");

                field = Math.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        } = 0.2f;

        public float Amplitude
        {
            get;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(Amplitude)} must be finite, but is {value}.");

                field = Math.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        } = 0.3f;

        public float Frequency
        {
            get;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(Frequency)} must be finite, but is {value}.");

                field = value;
                Invalidate(Invalidation.DrawNode);
            }
        } = 1.5f;

        public int Seed
        {
            get;
            set
            {
                field = value;
                Invalidate(Invalidation.DrawNode);
            }
        } = 1;

        private class CircularBlobDrawNode : SpriteDrawNode
        {
            public new CircularBlob Source => (CircularBlob)base.Source;

            public CircularBlobDrawNode(CircularBlob source)
                : base(source)
            {
            }

            private float innerRadius;
            private float texelSize;
            private float frequency;
            private float amplitude;
            private Vector2 noisePosition;
            private int seed = -1;

            public override void ApplyState()
            {
                base.ApplyState();

                innerRadius = Source.InnerRadius;
                frequency = Source.Frequency;
                amplitude = Source.Amplitude;

                int newSeed = Source.Seed;

                if (seed != newSeed)
                {
                    Random rand = new Random(newSeed);
                    noisePosition = new Vector2((float)(rand.NextDouble() * 1000), (float)(rand.NextDouble() * 1000));
                    seed = newSeed;
                }

                // smoothstep looks too sharp with 1px, let's give it a bit more
                texelSize = 1.5f / ScreenSpaceDrawQuad.Size.X;
            }

            private IUniformBuffer<CircularBlobParameters>? parametersBuffer;

            protected override void Blit(IRenderer renderer)
            {
                if (innerRadius == 0)
                    return;

                base.Blit(renderer);
            }

            protected override void BindUniformResources(IShader shader, IRenderer renderer)
            {
                base.BindUniformResources(shader, renderer);

                parametersBuffer ??= renderer.CreateUniformBuffer<CircularBlobParameters>();
                parametersBuffer.Data = new CircularBlobParameters
                {
                    InnerRadius = innerRadius,
                    TexelSize = texelSize,
                    Frequency = frequency,
                    Amplitude = amplitude,
                    NoisePosition = noisePosition,
                };

                shader.BindUniformBlock("m_CircularBlobParameters", parametersBuffer);
            }

            protected internal override bool CanDrawOpaqueInterior => false;

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                parametersBuffer?.Dispose();
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct CircularBlobParameters
            {
                public UniformFloat InnerRadius;
                public UniformFloat TexelSize;
                public UniformFloat Frequency;
                public UniformFloat Amplitude;
                public UniformVector2 NoisePosition;
                private readonly UniformPadding8 pad1;
            }
        }
    }
}
