// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkBindableNumber
    {
        private readonly BindableInt bindableNoPrecision = new BindableInt();
        private readonly BindableInt bindableWithPrecision = new BindableInt { Precision = 5 };

        [Benchmark]
        public void SetValueNoPrecision()
        {
            for (int i = 0; i < 1000; i++)
                bindableNoPrecision.Value = i;
        }

        [Benchmark]
        public void SetValueWithPrecision()
        {
            for (int i = 0; i < 1000; i++)
                bindableWithPrecision.Value = i;
        }
    }
}
