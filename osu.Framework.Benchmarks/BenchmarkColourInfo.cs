// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;

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
                yield return ColourInfo.SingleColour(SRGBColour.Transparent);
                yield return ColourInfo.SingleColour(SRGBColour.Cyan);
                yield return ColourInfo.SingleColour(SRGBColour.DarkGray);
            }
        }

        [Benchmark]
        public SRGBColour ConvertToSRGBColour() => Colour;

        [Benchmark]
        public Colour4 ConvertToColour4() => ((SRGBColour)Colour).ToLinear().Linear;

        [Benchmark]
        public Colour4 ExtractAndConvertToColour4()
        {
            Colour.TryExtractSingleColour(out SRGBColour colour);
            return colour.ToLinear().Linear;
        }
    }
}
