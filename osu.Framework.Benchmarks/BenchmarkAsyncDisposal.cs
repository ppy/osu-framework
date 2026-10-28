// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public partial class BenchmarkAsyncDisposal
    {
        private readonly List<Drawable> objects = new List<Drawable>();

        [GlobalSetup]
        [OneTimeSetUp]
        public virtual void SetUp()
        {
            objects.Clear();
            for (int i = 0; i < 10000; i++)
                objects.Add(new Box());
        }

        [Test]
        [Benchmark]
        public void Run()
        {
            objects.ForEach(AsyncDisposalQueue.Enqueue);
            AsyncDisposalQueue.WaitForEmpty();
        }
    }
}
