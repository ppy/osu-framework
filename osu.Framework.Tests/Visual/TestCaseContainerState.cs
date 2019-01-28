// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("ensure valid container state in various scenarios")]
    public class TestCaseContainerState : TestCase
    {
        /// <summary>
        /// Tests if a drawable can be added to a container, removed, and then re-added to the same container.
        /// </summary>
        [Test]
        public void TestPreLoadReAdding()
        {
            var container = new Container();
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
                var unused = new Container
                {
                    // Container is an IReadOnlyList<T>, so Children can accept a Container.
                    // This further means that CompositeDrawable.AddInternal will try to add all of
                    // the children of the Container that was set to Children, which should throw an exception
                    Children = new Container { Child = new Container() }
                };
            });
        }

        /// <summary>
        /// The same as <see cref="TestPreLoadMultipleAdds"/> however instead runs after the container is loaded.
        /// </summary>
        [Test]
        public void TestLoadedMultipleAdds()
        {
            AddAssert("Test loaded multiple adds", () =>
            {
                var loadedContainer = new Container();
                Add(loadedContainer);

                try
                {
                    loadedContainer.Add(new Container
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

        /// <summary>
        /// Tests whether the result of a <see cref="Container{T}.Contains(T)"/> operation is valid between multiple containers.
        /// This tests whether the comparator + equality operation in <see cref="CompositeDrawable.IndexOfInternal(Graphics.Drawable)"/> is valid.
        /// </summary>
        [Test]
        public void TestContainerContains()
        {
            var drawableA = new Sprite();
            var drawableB = new Sprite();
            var containerA = new Container { Child = drawableA };
            var containerB = new Container { Child = drawableB };

            var newContainer = new Container<Container> { Children = new[] { containerA, containerB } };

            // Because drawableA and drawableB have been added to separate containers,
            // they will both have Depth = 0 and ChildID = 1, which leads to edge cases if a
            // sorting comparer that doesn't compare references is used for Contains().
            // If this is not handled properly, it may have devastating effects in, e.g. Remove().

            Assert.IsTrue(newContainer.First(c => c.Contains(drawableA)) == containerA);
            Assert.IsTrue(newContainer.First(c => c.Contains(drawableB)) == containerB);

            Assert.DoesNotThrow(() => newContainer.First(c => c.Contains(drawableA)).Remove(drawableA));
            Assert.DoesNotThrow(() => newContainer.First(c => c.Contains(drawableB)).Remove(drawableB));
        }
    }
}
