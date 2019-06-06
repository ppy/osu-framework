// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Tests.Visual.Containers
{
    [System.ComponentModel.Description("ensure valid container state in various scenarios")]
    public class TestSceneContainerState : FrameworkTestScene
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

        [Test]
        public void TestChildrenRemovedOnClearInternal()
        {
            var drawableA = new Sprite();
            var drawableB = new Sprite();
            var drawableC = new Sprite();
            var containerA = new Container { Child = drawableC };

            var targetContainer = new Container { Children = new Drawable[] { drawableA, drawableB, containerA } };

            Assert.That(targetContainer, Has.Count.Not.Zero);

            targetContainer.ClearInternal();

            // Immediate children removed
            Assert.That(targetContainer, Has.Count.Zero);

            // Nested container's children not removed
            Assert.That(containerA, Has.Count.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestUnbindOnClearInternal(bool shouldDispose)
        {
            bool unbound = false;

            var drawableA = new Sprite().With(d => { d.OnUnbindAllBindables += () => unbound = true; });

            var container = new Container { Children = new[] { drawableA } };

            container.ClearInternal(shouldDispose);

            Assert.That(container, Has.Count.Zero);
            Assert.That(unbound, Is.EqualTo(shouldDispose));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDisposeOnClearInternal(bool shouldDispose)
        {
            bool disposed = false;

            var drawableA = new Sprite().With(d => { d.OnDispose += () => disposed = true; });

            var container = new Container { Children = new[] { drawableA } };

            Assert.That(container, Has.Count.Not.Zero);

            container.ClearInternal(shouldDispose);

            Assert.That(container, Has.Count.Zero);

            // Disposal happens asynchronously
            int iterations = 20;

            while (iterations-- > 0)
            {
                if (disposed)
                    break;

                Thread.Sleep(100);
            }

            Assert.That(disposed, Is.EqualTo(shouldDispose));
        }
    }
}
