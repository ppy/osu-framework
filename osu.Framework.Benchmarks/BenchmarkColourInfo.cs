// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Colour;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkColourInfo
    {
        private ColourInfo colourInfo;

        [GlobalSetup]
        public void GlobalSetup()
        {
            colourInfo = ColourInfo.SingleColour(Color4.Transparent);
        }

        [Benchmark]
        public SRGBColour ConvertToSRGBColour() => colourInfo;

        [Benchmark]
        public Color4 ConvertToColor4() => ((SRGBColour)colourInfo).Linear;

        [Benchmark]
        public Color4 ExtractAndConvertToColor4()
        {
            colourInfo.TryExtractSingleColour(out SRGBColour colour);
            return colour.Linear;
        }
    }
}
