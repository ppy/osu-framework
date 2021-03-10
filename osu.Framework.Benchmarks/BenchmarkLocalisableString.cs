// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Localisation;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkLocalisableString
    {
        private LocalisableString str1;
        private LocalisableString str2;

        [GlobalSetup]
        public void GlobalSetup()
        {
            str1 = new RomanisableString("a", "b");
            str2 = new RomanisableString("c", "d");
        }

        [Benchmark]
        public bool BenchmarkEquals() => str1.Equals(str2);

        [Benchmark]
        public int BenchmarkGetHashCode() => str1.GetHashCode();
    }
}
