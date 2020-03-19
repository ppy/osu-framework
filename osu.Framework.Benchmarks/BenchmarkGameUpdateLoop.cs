// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK.Graphics;

namespace osu.Framework.Benchmarks
{
    [TestFixture]
    [MemoryDiagnoser]
    public class BenchmarkGameUpdateLoop
    {
        private ManualGameHost gameHost;

        [GlobalSetup]
        [OneTimeSetUp]
        public virtual void SetUp()
        {
            gameHost = new ManualGameHost(new PoopGame());
        }

        [Test]
        [Benchmark]
        public void RunBenchmark() => gameHost.RunSingleFrame();

        [GlobalCleanup]
        [OneTimeTearDown]
        public virtual void TearDown()
        {
            gameHost?.Exit();
            gameHost?.Dispose();
        }

        private class PoopGame : Game
        {
            protected override void LoadComplete()
            {
                base.LoadComplete();

                for (int i = 0; i < 1000; i++)
                {
                    var box = new Box
                    {
                        Colour = Color4.Black,
                        RelativeSizeAxes = Axes.Both,
                    };
                    Add(box);
                    box.Spin(200, RotationDirection.Clockwise);
                }
            }
        }

        /// <summary>
        /// Ad headless host for testing purposes. Contains an arbitrary game that is running after construction.
        /// </summary>
        public class ManualGameHost : HeadlessGameHost
        {
            private ManualThreadRunner threadRunner;

            public ManualGameHost(Game runnableGame)
                : base("manual")
            {
                Task.Run(() =>
                {
                    try
                    {
                        Run(runnableGame);
                    }
                    catch
                    {
                        // may throw an unobserved exception if we don't handle here.
                    }
                });

                while (threadRunner == null || !threadRunner.HasRunOnce.IsSet)
                    Thread.Sleep(10);
            }

            protected override void Dispose(bool isDisposing)
            {
                threadRunner.HasRunOnce.Reset();
                base.Dispose(isDisposing);
            }

            public void RunSingleFrame() => threadRunner.RunSingleFrame();

            protected override ThreadRunner CreateThreadRunner(InputThread mainThread) => threadRunner = new ManualThreadRunner(mainThread);
        }

        private class ManualThreadRunner : ThreadRunner
        {
            public readonly ManualResetEventSlim HasRunOnce = new ManualResetEventSlim();

            public ManualThreadRunner(InputThread mainThread)
                : base(mainThread)
            {
            }

            public void RunSingleFrame()
            {
                ExecutionMode = ExecutionMode.SingleThread;
                base.RunMainLoop();
            }

            public override void RunMainLoop()
            {
                ExecutionMode = ExecutionMode.SingleThread;

                if (HasRunOnce.IsSet)
                    return;

                base.RunMainLoop();
                HasRunOnce.Set();
            }
        }
    }
}
