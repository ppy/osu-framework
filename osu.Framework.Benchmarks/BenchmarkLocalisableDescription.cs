// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions;
using osu.Framework.Localisation;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkLocalisableDescription
    {
        private LocalisableString[] descriptions = null!;

        [Params(1, 10, 100, 1000)]
        public int Times { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            descriptions = new LocalisableString[Times];
        }

        [Benchmark]
        public LocalisableString[] GetLocalisableDescription()
        {
            for (int i = 0; i < Times; i++)
                descriptions[i] = TestLocalisableEnum.One.GetLocalisableDescription();

            return descriptions;
        }

        private enum TestLocalisableEnum
        {
            [LocalisableDescription(typeof(TestStrings), nameof(TestStrings.One))]
            One,
        }

        private static class TestStrings
        {
            public static LocalisableString One => "1";
        }
    }
}
