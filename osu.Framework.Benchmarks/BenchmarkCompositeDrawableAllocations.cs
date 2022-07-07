// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkCompositeDrawableAllocations : GameBenchmark
    {
        private TestGame game = null!;

        [Test]
        [Benchmark]
        public void TestEmptyGameLoop()
        {
            game.Mode = 0;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestAllocContainer()
        {
            game.Mode = 1;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestLoadedContainer()
        {
            game.Mode = 2;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestAllocSprite()
        {
            game.Mode = 3;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestLoadedSprite()
        {
            game.Mode = 4;
            RunSingleFrame();
        }

        protected override Game CreateGame() => game = new TestGame();

        private class TestGame : Game
        {
            public int Mode;

            protected override void Update()
            {
                base.Update();

                Drawable? drawable = null;

                switch (Mode)
                {
                    case 0:
                        break;

                    case 1:
                    case 2:
                        drawable = new Container();
                        break;

                    case 3:
                    case 4:
                        drawable = new Sprite();
                        break;
                }

                if (Mode == 2 || Mode == 4)
                    LoadComponent(drawable);
            }
        }
    }
}
