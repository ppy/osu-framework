// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkPoolableDrawable : GameBenchmark
    {
        [Test]
        [Benchmark]
        public void RunFrame() => RunSingleFrame();

        protected override Game CreateGame() => new TestGame();

        private class TestGame : Game
        {
            private readonly TestPool pool;

            public TestGame()
            {
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        pool = new TestPool()
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                if (pool.CountInUse == 0)
                    Add(pool.Get());
            }
        }

        private class TestPool : DrawablePool<TestDrawable>
        {
            public TestPool()
                : base(10, 10)
            {
            }

            protected override TestDrawable CreateNewDrawable() => new TestDrawable();
        }

        private class TestDrawable : PoolableDrawable
        {
            protected override void PrepareForUse() => Expire();
        }
    }
}
