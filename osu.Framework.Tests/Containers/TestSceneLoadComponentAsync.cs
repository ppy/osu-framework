// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
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
        [Test]
        public void TestEnumerableOnlyInvokedOnce()
        {
            int invocationCount = 0;

            IEnumerable<AsyncChildLoadingComposite> composite = getEnumerableComponent(() =>
            {
                invocationCount++;

                var result = new AsyncChildLoadingComposite();
                result.AllowChildLoad();

                return result;
            });

            AddStep("clear all children", () => Clear());

            AddStep("load async", () => LoadComponentsAsync(composite, AddRange));

            AddUntilStep("component loaded", () => Children.Count == 1);

            AddAssert("invocation count is 1", () => invocationCount == 1);
        }

        private IEnumerable<AsyncChildLoadingComposite> getEnumerableComponent(Func<AsyncChildLoadingComposite> createComponent)
        {
            yield return createComponent();
        }

        [Test]
        public void TestUnpublishedChildDisposal()
        {
            AsyncChildLoadingComposite composite = null;

            AddStep("Add new composite", () => { Child = composite = new AsyncChildLoadingComposite(); });

            AddStep("Allow load", () => composite.AllowChildLoad());

            AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            AddStep("Dispose composite", Clear);

            AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
        }

        [Test]
        public void TestUnpublishedChildLoadBlockDisposal()
        {
            AsyncChildLoadingComposite composite = null;

            AddStep("Add new composite", () => { Child = composite = new AsyncChildLoadingComposite(); });

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

        [Test]
        public void TestDisposalDuringAsyncLoad()
        {
            AsyncChildrenLoadingComposite composite = null;

            AddStep("Add new composite", () => { Child = composite = new AsyncChildrenLoadingComposite(); });

            AddStep("Dispose child 2", () => composite.AsyncChild2.Dispose());

            AddStep("Allow child 1 load", () => composite.AllowChild1Load());

            AddUntilStep("Wait for child load", () => composite.AsyncChild1.LoadState == LoadState.Ready);

            AddUntilStep("Wait for loaded callback", () => composite.LoadedChildren != null);

            AddAssert("Only child1 loaded", () => composite.LoadedChildren.Count() == 1
                                                  && composite.LoadedChildren.First() == composite.AsyncChild1);
        }

        private class AsyncChildrenLoadingComposite : CompositeDrawable
        {
            public IEnumerable<TestLoadBlockingDrawable> LoadedChildren;

            public TestLoadBlockingDrawable AsyncChild1 { get; } = new TestLoadBlockingDrawable();

            public TestLoadBlockingDrawable AsyncChild2 { get; } = new TestLoadBlockingDrawable();

            public bool AsyncChild1LoadBegan => AsyncChild1.LoadState > LoadState.NotLoaded;

            public void AllowChild1Load() => AsyncChild1.AllowLoad.Set();

            public void AllowChild2Load() => AsyncChild2.AllowLoad.Set();

            public new bool IsDisposed => base.IsDisposed;

            protected override void LoadComplete()
            {
                // load but never add to hierarchy
                LoadComponentsAsync(new[] { AsyncChild1, AsyncChild2 }, loadComplete);

                base.LoadComplete();
            }

            private void loadComplete(IEnumerable<TestLoadBlockingDrawable> loadedChildren) => LoadedChildren = loadedChildren;
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
