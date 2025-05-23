// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    [WarmupCount(0)]
    [IterationCount(2)]
    public class BenchmarkBindableList
    {
        private static readonly int[] small_data = Enumerable.Range(0, 10).ToArray();

        [Params(0, 1, 10, 20)]
        public int NumBindings { get; set; }

        private BindableList<int>[] lists = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            lists = new BindableList<int>[NumBindings + 1];

            lists[0] = new BindableList<int>(Enumerable.Range(0, 10000).ToArray());
            for (int i = 1; i < lists.Length; i++)
                lists[i] = lists[i - 1].GetBoundCopy();
            lists[0].Clear();
        }

        [Benchmark(Baseline = true)]
        public void Create()
        {
            setupList();
        }

        [Benchmark]
        public void Add()
        {
            setupList().Add(1);
        }

        [Benchmark]
        public void Remove()
        {
            setupList().Remove(0);
        }

        [Benchmark]
        public void Clear()
        {
            setupList().Clear();
        }

        [Benchmark]
        public void AddRange()
        {
            setupList().AddRange(small_data);
        }

        [Benchmark]
        public void SetIndex()
        {
            setupList()[0]++;
        }

        [Benchmark]
        public int Enumerate()
        {
            int result = 0;

            foreach (int val in setupList())
                result += val;

            return result;
        }

        private BindableList<int> setupList()
        {
            lists[0].Clear();
            lists[0].Add(0);
            return lists[0];
        }
    }
}
