// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Utils;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkInterpolation : BenchmarkTest
    {
        [Benchmark]
        public MarginPadding InterpolateMarginPadding()
        {
            var first = new MarginPadding();
            var second = new MarginPadding(10);

            return Interpolation.ValueAt(0.5, first, second, 0, 1, Easing.OutQuint);
        }

        [Benchmark]
        public float InterpolateFloatGeneric()
        {
            const float first = 0;
            const float second = 10;

            return Interpolation.ValueAt<float>(0.5, first, second, 0, 1, Easing.OutQuint);
        }

        [Benchmark]
        public float InterpolateFloat()
        {
            const float first = 0;
            const float second = 10;

            return Interpolation.ValueAt(0.5, first, second, 0, 1, Easing.OutQuint);
        }
    }
}
