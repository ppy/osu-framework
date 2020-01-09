// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Lists;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkWeakList : BenchmarkTest
    {
        private readonly object[] objects = new object[1000];
        private WeakList<object> weakList;

        public override void SetUp()
        {
            for (int i = 0; i < 1000; i++)
                objects[i] = new object();
        }

        [IterationSetup]
        [SetUp]
        public void IterationSetup()
        {
            weakList = new WeakList<object>();

            foreach (var obj in objects)
                weakList.Add(obj);
        }

        [Benchmark]
        public void Add() => weakList.Add(new object());

        [Benchmark]
        public void Remove() => weakList.Remove(objects[0]);

        [Benchmark]
        public void RemoveEach()
        {
            foreach (var obj in objects)
                weakList.Remove(obj);
        }

        [Benchmark]
        public void Clear() => weakList.Clear();

        [Benchmark]
        public bool Contains() => weakList.Contains(objects[0]);

        [Benchmark]
        public object[] Enumerate() => weakList.ToArray();

        [Benchmark]
        public object[] ClearAndEnumerate()
        {
            weakList.Clear();
            return weakList.ToArray();
        }
    }
}
