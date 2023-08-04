// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public partial class BenchmarkSpriteText : GameBenchmark
    {
        private TestGame game = null!;

        [Test]
        [Benchmark]
        public void TestStaticText()
        {
            game.Mode = 0;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestMovingText()
        {
            game.Mode = 1;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestChangingText()
        {
            game.Mode = 2;
            RunSingleFrame();
        }

        protected override Game CreateGame() => game = new TestGame();

        private partial class TestGame : Game
        {
            public int Mode;

            private readonly string text1 = Guid.NewGuid().ToString();
            private readonly string text2 = Guid.NewGuid().ToString();

            private long frame;

            [BackgroundDependencyLoader]
            private void load()
            {
                for (int i = 0; i < 1000; i++)
                {
                    Add(new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "i am some relatively long text",
                    });
                }
            }

            protected override void Update()
            {
                base.Update();

                switch (Mode)
                {
                    case 0:
                        break;

                    case 1:
                        var pos = new Vector2(RNG.NextSingle(100));

                        foreach (var text in Children.OfType<SpriteText>())
                            text.Position = pos;

                        break;

                    case 2:
                        foreach (var text in Children.OfType<SpriteText>())
                            text.Text = frame % 2 == 0 ? text1 : text2;
                        break;
                }

                frame++;
            }
        }
    }
}
