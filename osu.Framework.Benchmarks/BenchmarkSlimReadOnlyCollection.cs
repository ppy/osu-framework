// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions.ListExtensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkSlimReadOnlyCollection
    {
        private readonly List<int> list = new List<int> { 0, 1, 2, 3, 4, 5, 3, 2, 3, 1, 4, 5, -1 };

        [Benchmark(Baseline = true)]
        public int List()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in list)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int ListAsReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in list.AsReadOnly())
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int ListAsSlimReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in list.AsSlimReadOnly())
                    sum += v;
            }

            return sum;
        }
    }
}
