// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public partial class BenchmarkUnbindAllBindables
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

        [Test]
        [Benchmark]
        public void TestComplexComposite()
        {
            var _ = new ComplexComposite();
            _.Load(clock, dependencies);
            _.UnbindAllBindables();
        }

        public partial class SimpleComposite3 : SimpleComposite2
        {
        }

        public partial class SimpleComposite2 : SimpleComposite1
        {
        }

        public partial class SimpleComposite1 : SimpleComposite
        {
        }

        public partial class SimpleComposite : CompositeDrawable
        {
        }

        public partial class ComplexComposite : CompositeDrawable
        {
            private readonly Bindable<int> bindable1 = new Bindable<int>();
            private readonly Bindable<int> bindable2 = new Bindable<int>();
            private readonly Bindable<int> bindable3 = new Bindable<int>();
            private readonly Bindable<int> bindable4 = new Bindable<int>();
            private readonly Bindable<int> bindable5 = new Bindable<int>();
            private readonly Bindable<int> bindable6 = new Bindable<int>();
            private readonly Bindable<int> bindable7 = new Bindable<int>();
            private readonly Bindable<int> bindable8 = new Bindable<int>();
            private readonly Bindable<int> bindable9 = new Bindable<int>();
            private readonly Bindable<int> bindable10 = new Bindable<int>();
        }
    }
}
