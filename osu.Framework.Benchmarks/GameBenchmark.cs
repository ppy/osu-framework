// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Benchmarks
{
    [TestFixture]
    [MemoryDiagnoser]
    public abstract class GameBenchmark
    {
        private ManualGameHost gameHost = null!;

        protected Game Game { get; private set; } = null!;

        [GlobalSetup]
        [OneTimeSetUp]
        public virtual void SetUp()
        {
            gameHost = new ManualGameHost(Game = CreateGame());
        }

        [GlobalCleanup]
        [OneTimeTearDown]
        public virtual void TearDown()
        {
            gameHost.Exit();
            gameHost.Dispose();
        }

        /// <summary>
        /// Runs a single game frame.
        /// </summary>
        protected void RunSingleFrame() => gameHost.RunSingleFrame();

        /// <summary>
        /// Creates the game.
        /// </summary>
        protected abstract Game CreateGame();

        /// <summary>
        /// Ad headless host for testing purposes. Contains an arbitrary game that is running after construction.
        /// </summary>
        private class ManualGameHost : HeadlessGameHost
        {
            private ManualThreadRunner threadRunner;

            public ManualGameHost(Game runnableGame)
                : base("manual", new HostOptions())
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Run(runnableGame);
                    }
                    catch
                    {
                        // may throw an unobserved exception if we don't handle here.
                    }
                }, TaskCreationOptions.LongRunning);

                // wait for the game to initialise before continuing with the benchmark process.
                while (threadRunner?.HasRunOnce != true)
                    Thread.Sleep(10);
            }

            protected override void Dispose(bool isDisposing)
            {
                threadRunner.RunOnce.Set();
                base.Dispose(isDisposing);
            }

            public void RunSingleFrame() => threadRunner.RunSingleFrame();

            protected override ThreadRunner CreateThreadRunner(InputThread mainThread) => threadRunner = new ManualThreadRunner(mainThread);
        }

        private class ManualThreadRunner : ThreadRunner
        {
            /// <summary>
            /// This is used to delay the initialisation process until the headless input thread has run once.
            /// Does not get reset with subsequence runs.
            /// </summary>
            public bool HasRunOnce { get; private set; }

            /// <summary>
            /// Set this to run one frame on the headless input thread.
            /// This is used for the initialise and shutdown processes, whereas <see cref="RunSingleFrame"/> is used for the benchmark process.
            /// </summary>
            public readonly ManualResetEventSlim RunOnce = new ManualResetEventSlim();

            public ManualThreadRunner(InputThread mainThread)
                : base(mainThread)
            {
                RunOnce.Set();
            }

            public void RunSingleFrame()
            {
                ExecutionMode = ExecutionMode.SingleThread;
                base.RunMainLoop();
            }

            public override void RunMainLoop()
            {
                if (!RunOnce.Wait(10000))
                    throw new TimeoutException("Run request didn't arrive for a long time");

                RunSingleFrame();
                RunOnce.Reset();

                HasRunOnce = true;
            }
        }
    }
}
