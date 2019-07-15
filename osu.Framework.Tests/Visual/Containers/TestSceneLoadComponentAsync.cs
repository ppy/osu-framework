// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneLoadComponentAsync : FrameworkTestScene
    {
        [Test]
        public void TestUnpublishedChildStillDisposed()
        {
            AsyncChildLoadingComposite composite = null;

            AddStep("Add new composite", () => { Child = composite = new AsyncChildLoadingComposite(); });

            AddUntilStep("Wait for child load", () => composite.AsyncChild.LoadState == LoadState.Ready);

            AddStep("Dispose composite", Clear);

            AddUntilStep("Child was disposed", () => composite.AsyncChildDisposed);
        }

        private class AsyncChildLoadingComposite : CompositeDrawable
        {
            public Container AsyncChild { get; } = new Container();

            public bool AsyncChildDisposed { get; private set; }

            protected override void LoadComplete()
            {
                AsyncChild.OnDispose += () => AsyncChildDisposed = true;

                // load but never add to hierarchy
                LoadComponentAsync(AsyncChild);

                base.LoadComplete();
            }
        }
    }
}
