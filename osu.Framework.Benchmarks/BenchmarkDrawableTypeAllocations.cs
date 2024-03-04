// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public partial class BenchmarkDrawableTypeAllocations
    {
        [Test]
        [Benchmark]
        public void TestBaseline()
        {
            object _ = new object();
        }

        [Test]
        [Benchmark]
        public void TestSprite()
        {
            _ = new Sprite();
        }

        [Test]
        [Benchmark]
        public void TestCompositeDrawable()
        {
            _ = new SimpleComposite();
        }

        [Test]
        [Benchmark]
        public void TestContainer()
        {
            _ = new Container();
        }

        [Test]
        [Benchmark]
        public void TestSpriteText()
        {
            _ = new SpriteText();
        }

        public partial class SimpleComposite : CompositeDrawable
        {
        }
    }
}
