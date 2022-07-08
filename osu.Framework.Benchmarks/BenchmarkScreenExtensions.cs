// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Screens;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkScreenExtensions : GameBenchmark
    {
        private Screen testScreen = null!;

        [Test]
        [Benchmark]
        public void IsCurrentScreen() => testScreen.IsCurrentScreen();

        protected override Game CreateGame() => new TestGame(testScreen = new Screen());

        private class TestGame : Game
        {
            private readonly Screen screen;

            public TestGame(Screen screen)
            {
                this.screen = screen;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Add(new ScreenStack(screen));
            }
        }
    }
}
