// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    public class TestCaseContainerState : TestCase
    {
        public override string Description => "Ensuring a container's state is consistent in various scenarios.";

        private readonly Container container;

        public TestCaseContainerState()
        {
            Add(container = new Container());
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            testLoadedMultipleAdds();
        }

        /// <summary>
        /// Tests if a drawable can be added to a container, removed, and then re-added to the same container.
        /// </summary>
        [Test]
        public void TestPreLoadReAdding()
        {
            var sprite = new Sprite();

            // Add
            Assert.DoesNotThrow(() => container.Add(sprite));
            Assert.IsTrue(container.Contains(sprite));

            // Remove
            Assert.DoesNotThrow(() => container.Remove(sprite));
            Assert.IsFalse(container.Contains(sprite));

            // Re-add
            Assert.DoesNotThrow(() => container.Add(sprite));
            Assert.IsTrue(container.Contains(sprite));
        }

        /// <summary>
        /// Tests whether adding a child to multiple containers by abusing <see cref="Container{T}.Children"/>
        /// results in a <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void TestPreLoadMultipleAdds()
        {
            // Non-async
            Assert.Throws<InvalidOperationException>(() =>
            {
                container.Add(new Container
                {
                    // Container is an IReadOnlyList<T>, so Children can accept a Container.
                    // This further means that CompositeDrawable.AddInternal will try to add all of
                    // the children of the Container that was set to Children, which should throw an exception
                    Children = new Container { Child = new Container() }
                });
            });
        }
        
        /// <summary>
        /// The same as <see cref="TestPreLoadMultipleAdds"/> however instead runs after the container is loaded.
        /// </summary>
        private void testLoadedMultipleAdds()
        {
            AddAssert("Test loaded multiple adds", () =>
            {
                try
                {
                    container.Add(new Container
                    {
                        // Container is an IReadOnlyList<T>, so Children can accept a Container.
                        // This further means that CompositeDrawable.AddInternal will try to add all of
                        // the children of the Container that was set to Children, which should throw an exception
                        Children = new Container { Child = new Container() }
                    });

                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            });
        }
    }
}
