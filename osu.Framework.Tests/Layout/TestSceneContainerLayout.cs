// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Layout
{
    [HeadlessTest]
    public class TestSceneContainerLayout : FrameworkTestScene
    {
        /// <summary>
        /// Tests that auto-size is updated when a child becomes alive.
        /// </summary>
        [Test]
        public void TestContainerAutoSizeUpdatesWhenChildBecomesAlive()
        {
            Box box = null;
            Container parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new Container
                {
                    RemoveCompletedTransforms = false,
                    AutoSizeAxes = Axes.Both,
                    Child = box = new Box
                    {
                        Size = new Vector2(200),
                        LifetimeStart = double.MaxValue
                    }
                };
            });

            AddStep("make child alive", () => box.LifetimeStart = double.MinValue);

            AddAssert("parent has size 200", () => Precision.AlmostEquals(new Vector2(200), parent.DrawSize));
        }

        /// <summary>
        /// Tests that auto-size is updated when a child is removed through death.
        /// </summary>
        [Test]
        public void TestContainerAutoSizeUpdatesWhenChildBecomesDead()
        {
            Box box = null;
            Container parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Child = box = new Box { Size = new Vector2(200) }
                };
            });

            AddStep("make child dead", () => box.Expire());

            AddAssert("parent has size 0", () => Precision.AlmostEquals(Vector2.Zero, parent.DrawSize));
        }

        /// <summary>
        /// Tests that auto-size is updated when a child becomes dead and doesn't get removed.
        /// </summary>
        [Test]
        public void TestContainerAutoSizeUpdatesWhenChildBecomesDeadWithoutRemoval()
        {
            Box box = null;
            Container parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new Container
                {
                    RemoveCompletedTransforms = false,
                    AutoSizeAxes = Axes.Both,
                    Child = box = new TestBox1 { Size = new Vector2(200) }
                };
            });

            AddStep("make child dead", () => box.Expire());

            AddAssert("parent has size 0", () => Precision.AlmostEquals(Vector2.Zero, parent.DrawSize));
        }

        /// <summary>
        /// Tests that auto-size properly captures a child's presence change.
        /// </summary>
        [Test]
        public void TestAddHiddenChildAndFadeIn()
        {
            Container content = null;
            Box child = null;

            AddStep("create test", () =>
            {
                Child = content = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                };
            });

            AddStep("add child", () => LoadComponentAsync(child = new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 500,
            }, d =>
            {
                content.Add(d);
                d.FadeInFromZero(50000);
            }));

            AddUntilStep("wait for child load", () => child.IsLoaded);

            AddUntilStep("content height matches box height", () => Precision.AlmostEquals(content.DrawHeight, child.DrawHeight));
        }

        /// <summary>
        /// Tests that a parent container is not re-auto-sized when a child's size changes along the bypassed axes.
        /// </summary>
        /// <param name="axes">The bypassed axes that are bypassed.</param>
        [TestCase(Axes.X)]
        [TestCase(Axes.Y)]
        [TestCase(Axes.Both)]
        public void TestParentNotInvalidatedByBypassedSize(Axes axes)
        {
            Box child = null;

            bool autoSized = false;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Child = child = new Box { BypassAutoSizeAxes = axes }
                }.With(c => c.OnAutoSize += () => autoSized = true);
            });

            AddUntilStep("wait for autosize", () => autoSized);

            AddStep("adjust child size", () =>
            {
                autoSized = false;

                if (axes == Axes.Both)
                    child.Size = new Vector2(50);
                else if (axes == Axes.X)
                    child.Width = 50;
                else if (axes == Axes.Y)
                    child.Height = 50;
            });

            AddWaitStep("wait for autosize", 1);

            AddAssert("not autosized", () => !autoSized);
        }

        /// <summary>
        /// Tests that a parent container is not re-auto-sized when a child's position changes along the bypassed axes.
        /// </summary>
        /// <param name="axes">The bypassed axes that are bypassed.</param>
        [TestCase(Axes.X)]
        [TestCase(Axes.Y)]
        [TestCase(Axes.Both)]
        public void TestParentNotInvalidatedByBypassedPosition(Axes axes)
        {
            Box child = null;

            bool autoSized = false;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Child = child = new Box { BypassAutoSizeAxes = axes }
                }.With(c => c.OnAutoSize += () => autoSized = true);
            });

            AddUntilStep("wait for autosize", () => autoSized);

            AddStep("adjust child size", () =>
            {
                autoSized = false;

                if (axes == Axes.Both)
                    child.Position = new Vector2(50);
                else if (axes == Axes.X)
                    child.X = 50;
                else if (axes == Axes.Y)
                    child.Y = 50;
            });

            AddWaitStep("wait for autosize", 1);

            AddAssert("not autosized", () => !autoSized);
        }

        [TestCase(Axes.X)]
        [TestCase(Axes.Y)]
        [TestCase(Axes.Both)]
        public void TestParentSizeNotInvalidatedWhenChildGeometryInvalidated(Axes axes)
        {
            Drawable child = null;

            Invalidation invalidation = Invalidation.None;

            AddStep("create test", () =>
            {
                Child = new TestContainer1
                {
                    Child = child = new Box { Size = new Vector2(200) }
                }.With(c => c.Invalidated += i => invalidation = i);
            });

            AddStep("move child", () =>
            {
                invalidation = Invalidation.None;

                if (axes == Axes.Both)
                    child.Position = new Vector2(10);
                else if (axes == Axes.X)
                    child.X = 10;
                else if (axes == Axes.Y)
                    child.Y = 10;
            });

            AddAssert("parent only invalidated with geometry", () => invalidation == Invalidation.MiscGeometry);
        }

        [TestCase(Axes.X)]
        [TestCase(Axes.Y)]
        [TestCase(Axes.Both)]
        public void TestParentGeometryNotInvalidatedWhenChildSizeInvalidated(Axes axes)
        {
            Drawable child = null;

            Invalidation invalidation = Invalidation.None;

            AddStep("create test", () =>
            {
                Child = new TestContainer1
                {
                    Child = child = new Box { Size = new Vector2(200) }
                }.With(c => c.Invalidated += i => invalidation = i);
            });

            AddStep("move child", () =>
            {
                invalidation = Invalidation.None;

                if (axes == Axes.Both)
                    child.Size = new Vector2(10);
                else if (axes == Axes.X)
                    child.Width = 10;
                else if (axes == Axes.Y)
                    child.Height = 10;
            });

            AddAssert("parent only invalidated with size", () => invalidation == Invalidation.DrawSize);
        }

        /// <summary>
        /// Tests that a child is not invalidated by its parent when not alive.
        /// </summary>
        [Test]
        public void TestChildNotInvalidatedWhenNotAlive()
        {
            Container parent = null;
            bool invalidated = false;

            AddStep("create test", () =>
            {
                Drawable child;

                Child = parent = new Container
                {
                    Size = new Vector2(200),
                    Child = child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        LifetimeStart = double.MaxValue
                    }
                };

                // Trigger a validation of draw size.
                Assert.That(child.DrawSize, Is.EqualTo(new Vector2(200)));

                child.Invalidated += _ => invalidated = true;
            });

            AddStep("resize parent", () => parent.Size = new Vector2(400));
            AddAssert("child not invalidated", () => !invalidated);
        }

        /// <summary>
        /// Tests that a loaded child is invalidated when it becomes alive.
        /// </summary>
        [Test]
        public void TestChildInvalidatedWhenMadeAlive()
        {
            Container parent = null;
            Drawable child = null;
            bool invalidated = false;

            AddStep("create test", () =>
            {
                Child = parent = new Container
                {
                    Size = new Vector2(200),
                    Child = child = new Box { RelativeSizeAxes = Axes.Both }
                };
            });

            AddStep("make child dead", () =>
            {
                child.LifetimeStart = double.MaxValue;
                child.Invalidated += _ => invalidated = true;
            });

            // See above: won't cause an invalidation
            AddStep("resize parent", () => parent.Size = new Vector2(400));

            AddStep("make child alive", () => child.LifetimeStart = double.MinValue);
            AddAssert("child invalidated", () => invalidated);

            // Final check to make sure that the correct invalidation occurred
            AddAssert("child size matches parent", () => child.DrawSize == parent.Size);
        }

        /// <summary>
        /// Tests that non-alive children always receive Parent invalidations.
        /// </summary>
        [Test]
        public void TestNonAliveChildReceivesParentInvalidations()
        {
            Container parent = null;
            bool invalidated = false;

            AddStep("create test", () =>
            {
                Drawable child;

                Child = parent = new Container
                {
                    Size = new Vector2(200),
                    Child = child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        LifetimeStart = double.MaxValue
                    }
                };

                child.Invalidated += _ => invalidated = true;
            });

            AddStep("invalidate parent", () =>
            {
                invalidated = false;
                parent.Invalidate(Invalidation.Parent);
            });

            AddAssert("child invalidated", () => invalidated);
        }

        /// <summary>
        /// Tests that DrawNode invalidations never propagate.
        /// </summary>
        [Test]
        public void TestDrawNodeInvalidationsNeverPropagate()
        {
            Container parent = null;
            bool invalidated = false;

            AddStep("create test", () =>
            {
                Drawable child;

                Child = parent = new Container
                {
                    Size = new Vector2(200),
                    Child = child = new Box { RelativeSizeAxes = Axes.Both }
                };

                child.Invalidated += _ => invalidated = true;
            });

            AddStep("invalidate parent", () =>
            {
                invalidated = false;
                parent.Invalidate(Invalidation.DrawNode);
            });

            AddAssert("child not invalidated", () => !invalidated);
        }

        private class TestBox1 : Box
        {
            public override bool RemoveWhenNotAlive => false;
        }

        private class TestContainer1 : Container
        {
            public new Action<Invalidation> Invalidated;

            protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
            {
                Invalidated?.Invoke(invalidation);
                return base.OnInvalidate(invalidation, source);
            }
        }
    }
}
