// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSingleFillFlowContainerAllocations : GameBenchmark
    {
        [Benchmark]
        public void RunFrame() => RunSingleFrame();

        protected override Game CreateGame() => new TestGame();

        private class TestGame : Game
        {
            private FillFlowContainer container;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                container = new FillFlowContainer
                {
                    Direction = FillDirection.Full,
                    AutoSizeAxes = Axes.Y,
                    Width = 1000
                };

                Add(container);
                for (int i = 0; i < 10000; i++)
                    container.Add(new Box { Size = new Vector2(50) });
            }

            protected override void Update()
            {
                container.Invalidate(source: Layout.InvalidationSource.Child);
                base.Update();
            }
        }
    }
}
