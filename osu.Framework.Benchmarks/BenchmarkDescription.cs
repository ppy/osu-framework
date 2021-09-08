// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkDescription
    {
        private string[] descriptions;

        [Params(1, 10, 100, 1000)]
        public int Times { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            descriptions = new string[Times];
        }

        [Benchmark]
        public string[] GetDescription()
        {
            for (int i = 0; i < Times; i++)
                descriptions[i] = TestLocalisableEnum.One.GetDescription();

            return descriptions;
        }

        private enum TestLocalisableEnum
        {
            [Description("test")]
            One,
        }
    }
}
