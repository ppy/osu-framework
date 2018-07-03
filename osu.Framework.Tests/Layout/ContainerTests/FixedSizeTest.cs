// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class FixedSizeTest
    {
        /// <summary>
        /// Tests that a fixed size container does not invalidate its size dependencies when a child is added.
        /// </summary>
        [Test]
        public void Test1()
        {
            var container = new LoadedContainer { Child = new LoadedBox() };

            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");
        }

        /// <summary>
        /// Tests that a fixed-size container does not invalidate its size dependencies when a child is removed.
        /// </summary>
        [Test]
        public void Test2()
        {
            LoadedBox child;
            // ReSharper disable once CollectionNeverQueried.Local : Keeping a local reference
            var container = new LoadedContainer { Child = child = new LoadedBox() };

            container.Remove(child);
            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");
        }

        private class LoadedContainer : Container
        {
            public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");

            public LoadedContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }

        private class LoadedBox : Box
        {
            public LoadedBox()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }
    }
}
