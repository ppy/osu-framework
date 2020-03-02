// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
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

        private class TestBox1 : Box
        {
            public override bool RemoveWhenNotAlive => false;
        }
    }
}
