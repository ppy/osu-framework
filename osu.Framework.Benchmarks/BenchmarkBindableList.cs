// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkBindableList
    {
        private readonly BindableList<int> list = new BindableList<int>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            for (int i = 0; i < 10; i++)
                list.Add(i);
        }

        [Benchmark]
        public int Enumerate()
        {
            int result = 0;

            for (int i = 0; i < 100; i++)
            {
                foreach (int val in list)
                    result += val;
            }

            return result;
        }
    }
}
