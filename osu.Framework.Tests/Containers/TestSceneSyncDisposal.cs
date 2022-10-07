// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public class TestSceneSyncDisposal : FrameworkTestScene
    {
        [Test]
        public void TestRemoveAndDisposeImmediatelyFromComposite()
        {
            TestComposite composite = null;

            AddStep("create composite", () => Child = composite = new TestComposite());

            AddAssert("immediate removal and disposal succeeds", () =>
            {
                composite.CompositeChild.RemoveAndDisposeImmediately();
                return composite.CompositeChild.Parent == null && composite.CompositeChild.IsDisposed && composite.InternalChildren.Count == 0;
            });
        }

        [Test]
        public void TestRemoveAndDisposeImmediatelyFromPlainContainer()
        {
            TestContainer container = null;

            AddStep("create container", () => Child = container = new TestContainer());

            AddAssert("immediate removal and disposal succeeds", () =>
            {
                container.ContainerChild.RemoveAndDisposeImmediately();
                return container.ContainerChild.Parent == null && container.ContainerChild.IsDisposed && container.InternalChildren.Count == 0;
            });
        }

        [Test]
        public void TestRemoveAndDisposeImmediatelyContentChildFromContainerWithOverriddenContent()
        {
            TestContainerWithCustomContent container = null;

            AddStep("create container", () => Child = container = new TestContainerWithCustomContent());

            AddAssert("immediate removal and disposal succeeds", () =>
            {
                container.ContentChild.RemoveAndDisposeImmediately();
                return container.ContentChild.Parent == null
                       && container.ContentChild.IsDisposed
                       && container.InternalChildren.Count == 2
                       && container.ContentContainer.InternalChildren.Count == 0;
            });
        }

        [Test]
        public void TestRemoveAndDisposeImmediatelyNonContentChildFromContainerWithOverriddenContent()
        {
            TestContainerWithCustomContent container = null;

            AddStep("create container", () => Child = container = new TestContainerWithCustomContent());

            AddAssert("immediate removal and disposal succeeds", () =>
            {
                container.NonContentChild.RemoveAndDisposeImmediately();
                return container.NonContentChild.Parent == null
                       && container.NonContentChild.IsDisposed
                       && container.InternalChildren.Count == 1
                       && container.ContentContainer.InternalChildren.Count == 1;
            });
        }

        [Test]
        public void TestRemoveAndDisposeImmediatelyUnattachedDrawable()
        {
            Container container = null;

            AddStep("create container", () => container = new Container());

            AddAssert("immediate removal and disposal succeeds", () =>
            {
                container.RemoveAndDisposeImmediately();
                return container.IsDisposed;
            });
        }

        private class TestComposite : CompositeDrawable
        {
            public readonly Drawable CompositeChild;

            public TestComposite()
            {
                InternalChild = CompositeChild = new Container();
            }
        }

        private class TestContainer : Container
        {
            public readonly Drawable ContainerChild;

            public TestContainer()
            {
                Child = ContainerChild = new Container();
            }
        }

        private class TestContainerWithCustomContent : Container
        {
            public readonly Drawable NonContentChild;
            public readonly Drawable ContentChild;
            public readonly Container ContentContainer;

            protected override Container<Drawable> Content => ContentContainer;

            public TestContainerWithCustomContent()
            {
                AddRangeInternal(new[]
                {
                    NonContentChild = new Container(),
                    ContentContainer = new Container
                    {
                        Child = ContentChild = new Container()
                    }
                });
            }
        }
    }
}
