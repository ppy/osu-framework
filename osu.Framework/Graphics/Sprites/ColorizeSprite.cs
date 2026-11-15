// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    public enum ColorizeAlgorithm
    {
        Multiply = 0,
        SetLum = 1,
    }

    public enum ColorizeRange
    {
        All = 0,
        Gray = 1,
        KeepWhite = 2,
        White = 3,
    }

    /// <summary>
    /// Recolours a texture in the fragment shader. Tint is a uniform so parent fade still uses <see cref="Drawable.Colour"/>.
    /// </summary>
    public partial class ColorizeSprite : Sprite
    {
        public const float DEFAULT_CHROMA_EPSILON = 1f / 255f;
        public const float DEFAULT_WHITE_LUMA = 0.92f;
        public const float DEFAULT_WHITE_CHROMA = 0.04f;

        public const float DEFAULT_WHITE_TOLERANCE = 0f;

        private Colour4 tint = Colour4.White;

        public Colour4 Tint
        {
            get => tint;
            set
            {
                if (tint == value)
                    return;

                tint = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private ColorizeAlgorithm algorithm = ColorizeAlgorithm.Multiply;

        public ColorizeAlgorithm Algorithm
        {
            get => algorithm;
            set
            {
                if (algorithm == value)
                    return;

                algorithm = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private ColorizeRange range = ColorizeRange.All;

        public ColorizeRange Range
        {
            get => range;
            set
            {
                if (range == value)
                    return;

                range = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float chromaEpsilon = DEFAULT_CHROMA_EPSILON;

        public float ChromaEpsilon
        {
            get => chromaEpsilon;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(ChromaEpsilon)} must be finite, but is {value}.");

                if (chromaEpsilon == value)
                    return;

                chromaEpsilon = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float whiteLuma = DEFAULT_WHITE_LUMA;

        public float WhiteLuma
        {
            get => whiteLuma;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(WhiteLuma)} must be finite, but is {value}.");

                if (whiteLuma == value)
                    return;

                whiteLuma = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float whiteChroma = DEFAULT_WHITE_CHROMA;

        public float WhiteChroma
        {
            get => whiteChroma;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(WhiteChroma)} must be finite, but is {value}.");

                if (whiteChroma == value)
                    return;

                whiteChroma = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float whiteTolerance = DEFAULT_WHITE_TOLERANCE;

        /// <summary>
        /// White range: 0–1 distance from RGB(1,1,1). 0 is exact white only; larger values mix near-white with a linear falloff.
        /// </summary>
        public float WhiteTolerance
        {
            get => whiteTolerance;
            set
            {
                if (!float.IsFinite(value))
                    throw new ArgumentException($"{nameof(WhiteTolerance)} must be finite, but is {value}.");

                float clamped = Math.Clamp(value, 0f, 1f);

                if (whiteTolerance == clamped)
                    return;

                whiteTolerance = clamped;
                Invalidate(Invalidation.DrawNode);
            }
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.COLORIZE);
        }

        protected override DrawNode CreateDrawNode() => new ColorizeSpriteDrawNode(this);

        protected class ColorizeSpriteDrawNode : SpriteDrawNode
        {
            public new ColorizeSprite Source => (ColorizeSprite)base.Source;

            public ColorizeSpriteDrawNode(ColorizeSprite source)
                : base(source)
            {
            }

            private Colour4 tint;
            private ColorizeAlgorithm algorithm;
            private ColorizeRange range;
            private float chromaEpsilon;
            private float whiteLuma;
            private float whiteChroma;
            private float whiteTolerance;

            private IUniformBuffer<ColorizeParameters> parametersBuffer;

            public override void ApplyState()
            {
                base.ApplyState();

                tint = Source.tint;
                algorithm = Source.algorithm;
                range = Source.range;
                chromaEpsilon = Source.chromaEpsilon;
                whiteLuma = Source.whiteLuma;
                whiteChroma = Source.whiteChroma;
                whiteTolerance = Source.whiteTolerance;
            }

            protected override void BindUniformResources(IShader shader, IRenderer renderer)
            {
                base.BindUniformResources(shader, renderer);

                parametersBuffer ??= renderer.CreateUniformBuffer<ColorizeParameters>();
                parametersBuffer.Data = new ColorizeParameters
                {
                    Tint = new Vector4(tint.R, tint.G, tint.B, tint.A),
                    Algorithm = (int)algorithm,
                    Range = (int)range,
                    ChromaEpsilon = chromaEpsilon,
                    WhiteLuma = whiteLuma,
                    WhiteChroma = whiteChroma,
                    WhiteTolerance = whiteTolerance,
                };

                shader.BindUniformBlock("m_ColorizeParameters", parametersBuffer);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                parametersBuffer?.Dispose();
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct ColorizeParameters
            {
                public UniformVector4 Tint;
                public UniformInt Algorithm;
                public UniformInt Range;
                public UniformFloat ChromaEpsilon;
                public UniformFloat WhiteLuma;
                public UniformFloat WhiteChroma;
                public UniformFloat WhiteTolerance;
                private readonly UniformPadding8 pad;
            }
        }
    }
}
