// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public class TestSceneLoadComponentAsync : FrameworkTestScene
    {
        private AsyncChildLoadingComposite composite;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Add new composite", () => { Child = composite = new AsyncChildLoadingComposite(); });
        }

        [Test]
        public void TestUnpublishedChildDisposal()
        {
            AddStep("Allow load", () => composite.AllowChildLoad());

            AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            AddStep("Dispose composite", Clear);

            AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
        }

        [Test]
        public void TestUnpublishedChildLoadBlockDisposal()
        {
            AddUntilStep("Wait for child load began", () => composite.AsyncChildLoadBegan);

            AddStep("Dispose composite", Clear);

            AddWaitStep("Wait for potential disposal", 50);

            AddAssert("Composite not yet disposed", () => !composite.IsDisposed);

            AddAssert("Child not yet disposed", () => !composite.AsyncChildDisposed);

            AddStep("Allow load", () => composite.AllowChildLoad());

            AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            AddUntilStep("Composite was disposed", () => composite.IsDisposed);

            AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
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
