// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSRGBColourMultiplication : BenchmarkTest
    {
        private static readonly SRGBColour white = SRGBColour.White;

        private static readonly SRGBColour white_with_opacity = SRGBColour.White.Opacity(0.5f);

        private static readonly SRGBColour gray = SRGBColour.Gray;

        private static readonly SRGBColour gray_light = SRGBColour.LightGray;

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
