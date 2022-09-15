// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkUnbindAllBindables
    {
        private readonly IFrameBasedClock clock = new FramedClock();
        private readonly IReadOnlyDependencyContainer dependencies = new DependencyContainer();

        [Test]
        [Benchmark]
        public void TestBaseline()
        {
            object _ = new object();
        }

        [Test]
        [Benchmark]
        public void TestCompositeDrawable()
        {
            var _ = new SimpleComposite();
            _.Load(clock, dependencies);
            _.UnbindAllBindables();
        }

        [Test]
        [Benchmark]
        public void TestContainer()
        {
            var _ = new Container();
            _.Load(clock, dependencies);
            _.UnbindAllBindables();
        }

        [Test]
        [Benchmark]
        public void TestTypeNestedComposite()
        {
            var _ = new SimpleComposite3();
            _.Load(clock, dependencies);
            _.UnbindAllBindables();
        }

        public class SimpleComposite3 : SimpleComposite2
        {
        }

        public class SimpleComposite2 : SimpleComposite1
        {
        }

        public class SimpleComposite1 : SimpleComposite
        {
        }

        public class SimpleComposite : CompositeDrawable
        {
        }
    }
}
