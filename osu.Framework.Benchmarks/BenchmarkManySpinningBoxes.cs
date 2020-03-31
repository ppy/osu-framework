// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkManySpinningBoxes : GameBenchmark
    {
        [Test]
        [Benchmark]
        public void RunFrame() => RunSingleFrame();

        protected override Game CreateGame() => new TestGame();

        private class TestGame : Game
        {
            protected override void LoadComplete()
            {
                base.LoadComplete();

                for (int i = 0; i < 1000; i++)
                {
                    var box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black
                    };

                    Add(box);

                    box.Spin(200, RotationDirection.Clockwise);
                }
            }
        }
    }
}
