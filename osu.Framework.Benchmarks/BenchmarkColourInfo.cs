// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Colour;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkColourInfo
    {
        [ParamsSource(nameof(ColourParams))]
        public ColourInfo Colour { get; set; }

        public IEnumerable<ColourInfo> ColourParams
        {
            get
            {
                yield return ColourInfo.SingleColour(Color4.Transparent);
                yield return ColourInfo.SingleColour(Color4.Cyan);
                yield return ColourInfo.SingleColour(Color4.DarkGray);
            }
        }

        [Benchmark]
        public SRGBColour ConvertToSRGBColour() => Colour;

        [Benchmark]
        public Color4 ConvertToColor4() => ((SRGBColour)Colour).Linear;

        [Benchmark]
        public Color4 ExtractAndConvertToColor4()
        {
            Colour.TryExtractSingleColour(out SRGBColour colour);
            return colour.Linear;
        }
    }
}
