// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using BenchmarkDotNet.Attributes;
using osu.Framework.Lists;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkWeakList : BenchmarkTest
    {
        [Params(1, 10, 100, 1000)]
        public int ItemCount { get; set; }

        private readonly object[] objects = new object[1000];
        private WeakList<object> weakList;

        public override void SetUp()
        {
            for (int i = 0; i < ItemCount; i++)
                objects[i] = new object();
        }

        [Benchmark(Baseline = true)]
        public void Add() => init();

        [Benchmark]
        public void RemoveOne()
        {
            init();

            weakList.Remove(objects[^1]);
        }

        [Benchmark]
        public void RemoveAllIteratively()
        {
            init();

            for (int i = 0; i < ItemCount; i++)
                weakList.Remove(objects[i]);
        }

        [Benchmark]
        public void Clear()
        {
            init();

            weakList.Clear();
        }

        [Benchmark]
        public bool Contains()
        {
            init();

            return weakList.Contains(objects[^1]);
        }

        [Benchmark]
        public object[] AddAndEnumerate()
        {
            init();

            return weakList.ToArray();
        }

        [Benchmark]
        public object[] ClearAndEnumerate()
        {
            init();

            weakList.Clear();
            return weakList.ToArray();
        }

        private void init()
        {
            weakList = new WeakList<object>();
            for (int i = 0; i < ItemCount; i++)
                weakList.Add(objects[i]);
        }
    }
}
