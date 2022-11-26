// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Benchmarks
{
    public partial class BenchmarkMakeChildAlive : GameBenchmark
    {
        [Test]
        [Benchmark]
        public void RunFrame() => RunSingleFrame();

        protected override Game CreateGame() => new TestGame();

        private partial class TestGame : Game
        {
            private readonly Box child;

            public TestGame()
            {
                child = new Box { RelativeSizeAxes = Axes.Both };
            }

            protected override void Update()
            {
                base.Update();

                Clear(false);
                Add(child);
            }
        }
    }
}
