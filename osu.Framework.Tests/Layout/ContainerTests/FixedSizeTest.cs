// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class FixedSizeTest : LayoutTest
    {
        /// <summary>
        /// Tests that a fixed size container does not invalidate its size dependencies when a child is added.
        /// </summary>
        [Test]
        public void Test1()
        {
            var container = new TestContainer();

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                container.Add(new Box());
                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that a fixed-size container does not invalidate its size dependencies when a child is removed.
        /// </summary>
        [Test]
        public void Test2()
        {
            Box child;
            var container = new TestContainer { Child = child = new Box() };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                container.Remove(child);
                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");

                return true;
            });
        }
    }
}
