// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Framework.Testing;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    [TestFixture]
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

        private LocalisableString concatenationViaNesting;
        private LocalisableString concatenationViaJoin;
        private TemporaryNativeStorage storage = null!;
        private LocalisationManager manager = null!;

        [GlobalSetup]
        [SetUp]
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

            const int part_count = 50;
            var parts = new List<LocalisableString>(part_count);
            LocalisableString? cursor = null;

            for (int i = 0; i <= part_count; ++i)
            {
                float f = (float)i / part_count;
                parts.Add(f.ToLocalisableString(@"N2"));
                cursor = cursor == null
                    ? f.ToLocalisableString(@"N2")
                    : LocalisableString.Interpolate($"{cursor.Value} {f:N2}");
            }

            concatenationViaNesting = cursor!.Value;
            concatenationViaJoin = LocalisableString.Join(" ", parts);
            storage = new TemporaryNativeStorage(Guid.NewGuid().ToString());
            manager = new LocalisationManager(new FrameworkConfigManager(storage));
        }

        [Benchmark]
        [Test(ExpectedResult = "0.00 0.02 0.04 0.06 0.08 0.10 0.12 0.14 0.16 0.18 0.20 0.22 0.24 0.26 0.28 0.30 0.32 0.34 0.36 0.38 0.40 0.42 0.44 0.46 0.48 0.50 0.52 0.54 0.56 0.58 0.60 0.62 0.64 0.66 0.68 0.70 0.72 0.74 0.76 0.78 0.80 0.82 0.84 0.86 0.88 0.90 0.92 0.94 0.96 0.98 1.00")]
        public string BenchmarkConcatenationViaNestingToString() => manager.GetLocalisedString(concatenationViaNesting);

        [Benchmark]
        [Test(ExpectedResult = "0.00 0.02 0.04 0.06 0.08 0.10 0.12 0.14 0.16 0.18 0.20 0.22 0.24 0.26 0.28 0.30 0.32 0.34 0.36 0.38 0.40 0.42 0.44 0.46 0.48 0.50 0.52 0.54 0.56 0.58 0.60 0.62 0.64 0.66 0.68 0.70 0.72 0.74 0.76 0.78 0.80 0.82 0.84 0.86 0.88 0.90 0.92 0.94 0.96 0.98 1.00")]
        public string BenchmarkConcatenationViaConcatToString() => manager.GetLocalisedString(concatenationViaJoin);

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
