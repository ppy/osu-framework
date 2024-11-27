// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Colour;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSRGBColourMultiplication : BenchmarkTest
    {
        private static readonly SRGBColour white = new SRGBColour
        {
            SRGB = new Color4(1f, 1f, 1f, 1f)
        };

        private static readonly SRGBColour white_with_opacity = new SRGBColour
        {
            SRGB = new Color4(1f, 1f, 1f, 0.5f)
        };

        private static readonly SRGBColour gray = new SRGBColour
        {
            SRGB = Color4.Gray
        };

        private static readonly SRGBColour gray_light = new SRGBColour
        {
            SRGB = Color4.LightGray
        };

        [Benchmark]
        public SRGBColour MultiplyNonWhite()
        {
            return gray * gray_light;
        }

        [Benchmark]
        public SRGBColour MultiplyWhite()
        {
            return gray * white;
        }

        [Benchmark]
        public SRGBColour MultiplyWhiteWithOpacity()
        {
            return gray * white_with_opacity;
        }

        [Benchmark]
        public SRGBColour MultiplyConstOne()
        {
            return gray * 1;
        }

        [Benchmark]
        public SRGBColour MultiplyConstNonOne()
        {
            return gray * 0.5f;
        }
    }
}
