// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkFillFlowContainerAllocations : GameBenchmark
    {
        private TestGame game = null!;

        [Benchmark]
        public void MultipleComputeLayoutPositions()
        {
            for (int i = 0; i < game.Containers.Count; i++)
            {
                game.Containers[i].TestComputeLayoutPositions();
            }
        }

        [Benchmark]
        public void SingleComputeLayoutPositions()
        {
            game.Containers[0].TestComputeLayoutPositions();
        }

        [Benchmark]
        public void MultipleWholeLayout()
        {
            game.Mode = 1;
            RunSingleFrame();
        }

        [Benchmark]
        public void SingleWholeLayout()
        {
            game.Mode = 0;
            RunSingleFrame();
        }

        [Benchmark]
        public void MultiplePerformLayout()
        {
            for (int i = 0; i < game.Containers.Count; i++)
            {
                game.Containers[i].TestPerformLayout();
            }
        }

        [Benchmark]
        public void SinglePerformLayout()
        {
            game.Containers[0].TestPerformLayout();
        }

        protected override Game CreateGame() => game = new TestGame();

        private class TestFillFlowContainer : FillFlowContainer
        {
            public void TestComputeLayoutPositions()
            {
                _ = ComputeLayoutPositions();
            }

            public void TestPerformLayout()
            {
                Invalidate(source: Layout.InvalidationSource.Child);
                UpdateAfterChildren();
            }
        }

        private class TestGame : Game
        {
            public readonly List<TestFillFlowContainer> Containers = new List<TestFillFlowContainer>();

            public int Mode;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                for (int i = 0; i < 100; i++)
                {
                    var container = new TestFillFlowContainer
                    {
                        Direction = FillDirection.Full,
                        AutoSizeAxes = Axes.Y,
                        Width = 1000
                    };
                    Containers.Add(container);
                    Add(container);

                    for (int j = 0; j < 1000; j++)
                        container.Add(new Box { Size = new Vector2(50) });
                }
            }

            protected override void Update()
            {
                base.Update();

                if (Mode == 0)
                {
                    Containers[0].Invalidate(source: Layout.InvalidationSource.Child);
                }
                else if (Mode == 1)
                {
                    for (int i = 0; i < Containers.Count; i++)
                    {
                        Containers[i].Invalidate(source: Layout.InvalidationSource.Child);
                    }
                }
            }
        }
    }
}
