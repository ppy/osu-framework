// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace osu.Framework.Benchmarks
{
    public class BenchmarkDependencyContainer : GameBenchmark
    {
        private Game game = null!;
        private TestBdlReceiver bdlReceiver = null!;
        private TestCachedReceiver cachedReceiver = null!;

        public override void SetUp()
        {
            base.SetUp();

            // Warm up caches to not pollute tests
            game.Dependencies.Inject(bdlReceiver = new TestBdlReceiver());
            game.Dependencies.Inject(cachedReceiver = new TestCachedReceiver());
            game.Dependencies.Get(typeof(Game));
            game.Dependencies.Get(typeof(CancellationToken?));
        }

        [Test]
        [Benchmark]
        public void Get() => game.Dependencies.Get(typeof(Game));

        [Test]
        [Benchmark]
        public void GetNullable() => game.Dependencies.Get(typeof(CancellationToken?));

        [Test]
        [Benchmark]
        public void InjectBdl() => game.Dependencies.Inject(bdlReceiver);

        [Test]
        [Benchmark]
        public void InjectCached() => game.Dependencies.Inject(cachedReceiver);

        protected override Game CreateGame() => game = new TestGame();

        private class TestBdlReceiver : Drawable
        {
            [UsedImplicitly] // params used implicitly
            [BackgroundDependencyLoader]
            private void load(Game game, TextureStore textures, AudioManager audio)
            {
            }
        }

        private class TestCachedReceiver : Drawable
        {
            [Resolved]
            private GameHost host { get; set; } = null!;

            [Resolved]
            private FrameworkConfigManager frameworkConfigManager { get; set; } = null!;

            [Resolved]
            private FrameworkDebugConfigManager frameworkDebugConfigManager { get; set; } = null!;
        }

        private class TestGame : Game
        {
        }
    }
}
