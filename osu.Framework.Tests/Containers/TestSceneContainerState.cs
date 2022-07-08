// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osuTK;

namespace osu.Framework.Tests.Containers
{
    [System.ComponentModel.Description("ensure valid container state in various scenarios")]
    [HeadlessTest]
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
        /// Tests whether adding a child to multiple containers results in a <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void TestPreLoadMultipleAdds()
        {
            // Non-async
            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused1 = new Container
                {
                    Child = new Container(),
                };

                var unused2 = new Container { Child = unused1.Child };
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
                    var unused = new Container
                    {
                        Child = new Container(),
                    };

                    loadedContainer.Add(new Container { Child = unused.Child });
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            });
        }

        /// <summary>
        /// Tests whether a drawable that is loaded can be added to an unloaded container.
        /// </summary>
        [Test]
        public void TestAddLoadedDrawableToUnloadedContainer()
        {
            Drawable target = null;

            AddStep("load target", () =>
            {
                Add(target = new Box { Size = new Vector2(100) });

                // Empty scheduler to force creation of the scheduler.
                target.Schedule(() => { });
            });

            AddStep("remove target", () => Remove(target));
            AddStep("add target to unloaded container", () => Add(new Container { Child = target }));
        }

        /// <summary>
        /// Tests whether the result of a <see cref="Container{T}.Contains(T)"/> operation is valid between multiple containers.
        /// This tests whether the comparator + equality operation in <see cref="CompositeDrawable.IndexOfInternal(Drawable)"/> is valid.
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

            GC.KeepAlive(drawableA);
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

            GC.KeepAlive(drawableA);
        }

        [Test]
        public void TestAsyncLoadClearWhileAsyncDisposing()
        {
            Container safeContainer = null;
            DelayedLoadDrawable drawable = null;

            // We are testing a disposal deadlock scenario. When the test runner exits, it will attempt to dispose the game hierarchy,
            // and will fall into the deadlocked state itself. For this reason an intermediate "safe" container is used, which is
            // removed from the hierarchy immediately after use and is thus not disposed when the test runner exits.
            // This does NOT free up the LoadComponentAsync thread pool for use by other tests - that thread is in a deadlocked state forever.
            AddStep("add safe container", () => Add(safeContainer = new Container()));

            // Get the drawable into an async loading state
            AddStep("begin async load", () =>
            {
                safeContainer.LoadComponentAsync(drawable = new DelayedLoadDrawable(), _ => { });
                Remove(safeContainer);
            });

            AddUntilStep("wait until loading", () => drawable.LoadState == LoadState.Loading);

            // Make the async disposal queue attempt to dispose the drawable
            AddStep("enqueue async disposal", () => AsyncDisposalQueue.Enqueue(drawable));
            AddWaitStep("wait for disposal task to run", 10);

            // Clear the contents of the drawable, causing a second async disposal
            AddStep("allow load", () => drawable.AllowLoad.Set());

            AddUntilStep("drawable was cleared successfully", () => drawable.HasCleared);
        }

        [Test]
        public void TestExpireChildAfterLoad()
        {
            Container container = null;
            Drawable child = null;

            AddStep("add container and child", () =>
            {
                Add(container = new Container
                {
                    Child = child = new Box()
                });
            });

            AddStep("expire child", () => child.Expire());

            AddUntilStep("container has no children", () => container.Count == 0);
        }

        [Test]
        public void TestExpireChildBeforeLoad()
        {
            Container container = null;

            AddStep("add container", () => Add(container = new Container()));

            AddStep("add expired child", () =>
            {
                var child = new Box();
                child.Expire();

                container.Add(child);
            });

            AddUntilStep("container has no children", () => container.Count == 0);
        }

        private class DelayedLoadDrawable : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            public bool HasCleared { get; private set; }

            public DelayedLoadDrawable()
            {
                InternalChild = new Box();
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();

                ClearInternal();

                HasCleared = true;
            }
        }

        [Test]
        public void TestAliveChangesDuringExpiry()
        {
            TestContainer container = null;

            int count = 0;

            void checkCount() => count = container.AliveInternalChildren.Count;

            AddStep("create container", () => Child = container = new TestContainer());

            AddStep("perform test", () =>
            {
                container.Add(new Box());
                container.Add(new Box());
                container.ScheduleAfterChildren(checkCount);
            });

            AddAssert("correct count", () => count == 2);

            AddStep("perform test", () =>
            {
                container.First().Expire();
                container.Add(new Box());
                container.ScheduleAfterChildren(checkCount);
            });

            AddAssert("correct count", () => count == 2);
        }

        [Test]
        public void TestAliveChildrenContainsOnlyAliveChildren()
        {
            Container container = null;
            Drawable aliveChild = null;
            Drawable nonAliveChild = null;

            AddStep("create container", () =>
            {
                Child = container = new Container
                {
                    Children = new[]
                    {
                        aliveChild = new Box(),
                        nonAliveChild = new Box { LifetimeStart = double.MaxValue }
                    }
                };
            });

            AddAssert("1 alive child", () => container.AliveChildren.Count == 1);
            AddAssert("alive child contained", () => container.AliveChildren.Contains(aliveChild));
            AddAssert("non-alive child not contained", () => !container.AliveChildren.Contains(nonAliveChild));
        }

        private class TestContainer : Container
        {
            public new void ScheduleAfterChildren(Action action) => SchedulerAfterChildren.AddDelayed(action, TransformDelay);
        }
    }
}
