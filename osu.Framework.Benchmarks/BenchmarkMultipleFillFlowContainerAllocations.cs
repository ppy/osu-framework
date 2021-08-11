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
    public class BenchmarkMultipleFillFlowContainerAllocations : GameBenchmark
    {
        [Benchmark]
        public void RunFrame() => RunSingleFrame();

        protected override Game CreateGame() => new TestGame();

        private class TestGame : Game
        {
            private readonly List<FillFlowContainer> containers = new List<FillFlowContainer>();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                for (int i = 0; i < 100; i++)
                {
                    var container = new FillFlowContainer
                    {
                        Direction = FillDirection.Full,
                        AutoSizeAxes = Axes.Y,
                        Width = 1000
                    };
                    containers.Add(container);
                    Add(container);

                    for (int j = 0; j < 1000; j++)
                        container.Add(new Box { Size = new Vector2(50) });
                }
            }

            protected override void Update()
            {
                for (int i = 0; i < containers.Count; i++)
                {
                    containers[i].Invalidate(source: Layout.InvalidationSource.Child);
                }

                base.Update();
            }
        }
    }
}
