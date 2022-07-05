// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkManySpinningBoxes : GameBenchmark
    {
        private TestGame game = null!;

        [Test]
        [Benchmark]
        public void RunFrame()
        {
            game.MainContent.AutoSizeAxes = Axes.None;
            game.MainContent.RelativeSizeAxes = Axes.Both;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void RunFrameWithAutoSize()
        {
            game.MainContent.RelativeSizeAxes = Axes.None;
            game.MainContent.AutoSizeAxes = Axes.Both;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void RunFrameWithAutoSizeDuration()
        {
            game.MainContent.RelativeSizeAxes = Axes.None;
            game.MainContent.AutoSizeAxes = Axes.Both;
            game.MainContent.AutoSizeDuration = 100;
            RunSingleFrame();
        }

        protected override Game CreateGame() => game = new TestGame();

        private class TestGame : Game
        {
            public Container MainContent = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Add(MainContent = new Container());

                for (int i = 0; i < 1000; i++)
                {
                    var box = new Box
                    {
                        Size = new Vector2(100),
                        Colour = Color4.Black
                    };

                    MainContent.Add(box);

                    box.Spin(200, RotationDirection.Clockwise);
                }
            }
        }
    }
}
