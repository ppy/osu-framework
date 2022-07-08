// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Localisation;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkLocalisableString
    {
        private string string1 = null!;
        private string string2 = null!;
        private LocalisableString localisableString1;
        private LocalisableString localisableString2;
        private LocalisableString romanisableString1;
        private LocalisableString romanisableString2;
        private LocalisableString translatableString1;
        private LocalisableString translatableString2;
        private LocalisableString formattableString1;
        private LocalisableString formattableString2;

        [GlobalSetup]
        public void GlobalSetup()
        {
            string1 = "a";
            string2 = "b";
            localisableString1 = "a";
            localisableString2 = "b";
            romanisableString1 = new RomanisableString("a", "b");
            romanisableString2 = new RomanisableString("c", "d");
            translatableString1 = new TranslatableString("e", "f");
            translatableString2 = new TranslatableString("g", "h");
            formattableString1 = LocalisableString.Format("({0})", "j");
            formattableString2 = LocalisableString.Format("[{0}]", "l");
        }

        [Benchmark]
        public bool BenchmarkStringEquals() => string1.Equals(string2, StringComparison.Ordinal);

        [Benchmark]
        public bool BenchmarkLocalisableStringEquals() => localisableString1.Equals(localisableString2);

        [Benchmark]
        public bool BenchmarkRomanisableEquals() => romanisableString1.Equals(romanisableString2);

        [Benchmark]
        public bool BenchmarkTranslatableEquals() => translatableString1.Equals(translatableString2);

        [Benchmark]
        public bool BenchmarkFormattableEquals() => formattableString1.Equals(formattableString2);

        [Benchmark]
        public int BenchmarkStringGetHashCode() => string1.GetHashCode();

        [Benchmark]
        public int BenchmarkLocalisableStringGetHashCode() => localisableString1.GetHashCode();

        [Benchmark]
        public int BenchmarkRomanisableGetHashCode() => romanisableString1.GetHashCode();

        [Benchmark]
        public int BenchmarkTranslatableGetHashCode() => translatableString1.GetHashCode();

        [Benchmark]
        public int BenchmarkFormattableGetHashCode() => formattableString1.GetHashCode();
    }
}
