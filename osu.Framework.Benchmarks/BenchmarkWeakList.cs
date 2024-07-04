// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        private WeakList<object> weakList = null!;

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
        public void RemoveAllStaggered()
        {
            init();

            if (ItemCount == 0)
                return;

            weakList.Remove(objects[ItemCount / 2]);

            for (int i = 1; i < ItemCount / 2; i++)
            {
                weakList.Remove(objects[ItemCount / 2 - i]);
                weakList.Remove(objects[ItemCount / 2 + i]);
            }
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

        [Benchmark]
        public void ManyAddsAndRemoveFromStartWithNoEnumeration()
        {
            weakList = new WeakList<object>();

            object obj = new object();
            weakList.Add(obj);

            for (int i = 0; i < ItemCount * 1000; i++)
            {
                weakList.Add(obj);
                weakList.RemoveAt(0);

                if (i % 1000 == 0)
                    GC.Collect();
            }
        }

        [Benchmark]
        public void ManyAddsAndRemoveFromEndWithNoEnumeration()
        {
            weakList = new WeakList<object>();

            object obj = new object();
            weakList.Add(obj);

            for (int i = 0; i < ItemCount * 1000; i++)
            {
                weakList.Add(obj);
                weakList.RemoveAt(1);

                if (i % 1000 == 0)
                    GC.Collect();
            }
        }

        [Benchmark]
        public void ManyAddsAndRemoveFromMiddleWithNoEnumeration()
        {
            weakList = new WeakList<object>();

            object obj = new object();
            object obj2 = new object();

            weakList.Add(new object());
            weakList.Add(obj2);
            weakList.Add(obj);

            for (int i = 0; i < ItemCount * 1000; i++)
            {
                weakList.Remove(i % 2 == 0 ? obj2 : obj);
                weakList.Add(i % 2 == 0 ? obj2 : obj);

                if (i % 1000 == 0)
                    GC.Collect();
            }
        }

        private void init()
        {
            weakList = new WeakList<object>();
            for (int i = 0; i < ItemCount; i++)
                weakList.Add(objects[i]);
        }
    }
}
