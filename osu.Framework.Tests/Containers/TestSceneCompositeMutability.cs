// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public class TestSceneCompositeMutability : FrameworkTestScene
    {
        [TestCase(TestThread.External, false)]
        [TestCase(TestThread.Update, false)]
        public void TestNotLoadedAdd(TestThread thread, bool shouldThrow) => performTest(LoadState.NotLoaded, thread, shouldThrow);

        [TestCase(TestThread.External, true)]
        [TestCase(TestThread.Update, true)]
        [TestCase(TestThread.Load, false)]
        public void TestLoadingAdd(TestThread thread, bool shouldThrow) => performTest(LoadState.Loading, thread, shouldThrow);

        [TestCase(TestThread.External, true)]
        [TestCase(TestThread.Update, false)]
        [TestCase(TestThread.Load, false)]
        public void TestReadyAdd(TestThread thread, bool shouldThrow) => performTest(LoadState.Ready, thread, shouldThrow);

        [TestCase(TestThread.External, true)]
        [TestCase(TestThread.Update, false)]
        public void TestLoadedAdd(TestThread thread, bool shouldThrow) => performTest(LoadState.Loaded, thread, shouldThrow);

        private void performTest(LoadState expectedState, TestThread thread, bool shouldThrow)
        {
            var container = new BlockableLoadContainer();

            // Load to the expected state
            if (expectedState > LoadState.NotLoaded)
            {
                LoadState stateToWaitFor = expectedState;

                AddStep("begin loading", () =>
                {
                    LoadComponentAsync(container, Add);

                    switch (expectedState)
                    {
                        // Special case for the load thread: the action is invoked after the event is set, so this is handled in the switch below
                        case LoadState.Loading when thread != TestThread.Load:
                            container.LoadingEvent.Set();
                            break;

                        // Special case for the load thread: possibly active during the ready state, but the ready event is handled in the switch below
                        case LoadState.Ready when thread == TestThread.Load:
                            container.LoadingEvent.Set();
                            stateToWaitFor = LoadState.Loading; // We'll never reach the ready state before the switch below
                            break;

                        case LoadState.Ready:
                            container.LoadingEvent.Set();
                            container.ReadyEvent.Set();
                            break;

                        case LoadState.Loaded:
                            container.LoadingEvent.Set();
                            container.ReadyEvent.Set();
                            container.LoadedEvent.Set();
                            break;
                    }
                });

                AddUntilStep($"wait for {stateToWaitFor} state", () => container.LoadState == stateToWaitFor);
            }

            bool hasResult = false;
            bool thrown = false;

            switch (thread)
            {
                case TestThread.External:
                {
                    AddStep("run external thread", () => new Thread(tryThrow) { IsBackground = true }.Start());
                    break;
                }

                case TestThread.Update:
                {
                    AddStep("schedule", () => Schedule(tryThrow));
                    break;
                }

                case TestThread.Load:
                {
                    switch (expectedState)
                    {
                        case LoadState.Loading:
                            AddStep("bind event", () => container.OnLoading += tryThrow);
                            AddStep("set loading", () => container.LoadingEvent.Set());
                            break;

                        case LoadState.Ready:
                            AddStep("bind event", () => container.OnReady += tryThrow);
                            AddStep("set loading", () => container.ReadyEvent.Set());
                            break;
                    }

                    break;
                }
            }

            AddUntilStep("wait for result", () => hasResult);
            AddAssert("thrown", () => thrown == shouldThrow);

            AddStep("allow load completion", () =>
            {
                container.LoadingEvent.Set();
                container.ReadyEvent.Set();
                container.LoadedEvent.Set();
            });

            void tryThrow()
            {
                try
                {
                    container.Add(new Box());
                    thrown = false;
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                hasResult = true;
            }
        }

        private class BlockableLoadContainer : Framework.Graphics.Containers.Container
        {
            /// <summary>
            /// Allows continuation to a point in the <see cref="LoadState.Loading"/> state which invokes <see cref="OnLoading"/>.
            /// </summary>
            public readonly ManualResetEventSlim LoadingEvent = new ManualResetEventSlim(false);

            /// <summary>
            /// Allows continuation to a <see cref="LoadState.Ready"/> state.
            /// </summary>
            public readonly ManualResetEventSlim ReadyEvent = new ManualResetEventSlim(false);

            /// <summary>
            /// Allows continuation to a <see cref="LoadState.Loaded"/> state.
            /// </summary>
            public readonly ManualResetEventSlim LoadedEvent = new ManualResetEventSlim(false);

            /// <summary>
            /// Invoked in the <see cref="LoadState.Loading"/> state.
            /// </summary>
            public Action OnLoading;

            /// <summary>
            /// Invoked in the <see cref="LoadState.Ready"/> state.
            /// </summary>
            public Action OnReady;

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!LoadingEvent.Wait(10000))
                    throw new TimeoutException("Load took too long");

                OnLoading?.Invoke();
                OnLoading = null;

                if (!ReadyEvent.Wait(10000))
                    throw new TimeoutException("Ready took too long");
            }

            public override bool UpdateSubTree()
            {
                OnReady?.Invoke();
                OnReady = null;

                if (!LoadedEvent.IsSet)
                    return false;

                return base.UpdateSubTree();
            }
        }

        public enum TestThread
        {
            External,
            Update,
            Load
        }
    }
}
