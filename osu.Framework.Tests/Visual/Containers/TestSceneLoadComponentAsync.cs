// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneLoadComponentAsync : FrameworkTestScene
    {
        private AsyncChildLoadingComposite composite;

        [SetUpSteps]
        public void SetUpSteps()
        {
            Steps.AddStep("Add new composite", () => { Child = composite = new AsyncChildLoadingComposite(); });
        }

        [Test]
        public void TestUnpublishedChildDisposal()
        {
            Steps.AddStep("Allow load", () => composite.AllowChildLoad());

            Steps.AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            Steps.AddStep("Dispose composite", Clear);

            Steps.AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
        }

        [Test]
        public void TestUnpublishedChildLoadBlockDisposal()
        {
            Steps.AddUntilStep("Wait for child load began", () => composite.AsyncChildLoadBegan);

            Steps.AddStep("Dispose composite", Clear);

            Steps.AddWaitStep("Wait for potential disposal", 50);

            Steps.AddAssert("Composite not yet disposed", () => !composite.IsDisposed);

            Steps.AddAssert("Child not yet disposed", () => !composite.AsyncChildDisposed);

            Steps.AddStep("Allow load", () => composite.AllowChildLoad());

            Steps.AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            Steps.AddUntilStep("Composite was disposed", () => composite.IsDisposed);

            Steps.AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
        }

        private class AsyncChildLoadingComposite : CompositeDrawable
        {
            public TestLoadBlockingDrawable AsyncChild { get; } = new TestLoadBlockingDrawable();

            public bool AsyncChildDisposed { get; private set; }

            public bool AsyncChildLoadBegan => AsyncChild.LoadState > LoadState.NotLoaded;

            public void AllowChildLoad() => AsyncChild.AllowLoad.Set();

            public new bool IsDisposed => base.IsDisposed;

            protected override void LoadComplete()
            {
                AsyncChild.OnDispose += () => AsyncChildDisposed = true;

                // load but never add to hierarchy
                LoadComponentAsync(AsyncChild);

                base.LoadComplete();
            }
        }

        private class TestLoadBlockingDrawable : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }
    }
}
