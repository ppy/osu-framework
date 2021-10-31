// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkAggregateBindable
    {
        private readonly BindableInt source1 = new BindableInt();
        private readonly BindableInt source2 = new BindableInt();

        [Benchmark]
        public void AggregateRecalculation()
        {
            var aggregate = new AggregateBindable<int>(((i, j) => i + j));

            aggregate.AddSource(source1);
            aggregate.AddSource(source2);

            for (int i = 0; i < 100; i++)
            {
                source1.Value = i;
                source2.Value = i;
            }
        }
    }
}
