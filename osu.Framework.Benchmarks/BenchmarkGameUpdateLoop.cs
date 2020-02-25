// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.IO;
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
            private readonly ManualResetEventSlim hasProcessed = new ManualResetEventSlim(false);

            public ManualGameHost(Game runnableGame)
                : base("manual", false, true, false)
            {
                RunningSingleThreaded = true;

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

                hasProcessed.Wait();
            }

            public void RunSingleFrame() => base.ProcessInputFrame();

            protected override void ProcessInputFrame()
            {
                InputThread.ActiveHz = 0;
                InputThread.InactiveHz = 0;

                if (!hasProcessed.IsSet)
                {
                    hasProcessed.Set();
                    RunSingleFrame();
                }

                Thread.Sleep(10);
            }

            protected override void PerformExit(bool immediately)
            {
                hasProcessed.Reset();
                base.PerformExit(immediately);
            }

            protected override void Dispose(bool isDisposing)
            {
                if (ExecutionState != ExecutionState.Stopped)
                    Exit();

                base.Dispose(isDisposing);
            }
        }
    }
}
