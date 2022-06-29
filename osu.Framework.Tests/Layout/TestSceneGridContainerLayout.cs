// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    public class TestSceneGridContainerLayout : FrameworkTestScene
    {
        /// <summary>
        /// Tests that a grid's auto-size is updated when a child becomes alive.
        /// </summary>
        [Test]
        public void TestGridContainerAutoSizeUpdatesWhenChildBecomesAlive()
        {
            Box box = null;
            GridContainer parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new GridContainer
                {
                    AutoSizeAxes = Axes.Both,
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            box = new Box
                            {
                                Size = new Vector2(200),
                                LifetimeStart = double.MaxValue
                            }
                        },
                    }
                };
            });

            AddStep("make child alive", () => box.LifetimeStart = double.MinValue);

            AddAssert("parent has size 200", () => Precision.AlmostEquals(new Vector2(200), parent.DrawSize));
        }

        /// <summary>
        /// Tests that a grid's auto-size is updated when a child is removed through death.
        /// </summary>
        [Test]
        public void TestGridContainerAutoSizeUpdatesWhenChildBecomesDead()
        {
            Box box = null;
            GridContainer parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new GridContainer
                {
                    AutoSizeAxes = Axes.Both,
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            box = new Box { Size = new Vector2(200) }
                        },
                    }
                };
            });

            AddStep("make child dead", () => box.Expire());

            AddAssert("parent has size 0", () => Precision.AlmostEquals(Vector2.Zero, parent.DrawSize));
        }

        /// <summary>
        /// Tests that a grid's auto-size is updated when a child becomes dead and doesn't get removed.
        /// </summary>
        [Test]
        public void TestGridContainerAutoSizeUpdatesWhenChildBecomesDeadWithoutRemoval()
        {
            Box box = null;
            GridContainer parent = null;

            AddStep("create test", () =>
            {
                Child = parent = new GridContainer
                {
                    AutoSizeAxes = Axes.Both,
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            box = new TestBox1 { Size = new Vector2(200) }
                        },
                    }
                };
            });

            AddStep("make child dead", () => box.Expire());

            AddAssert("parent has size 0", () => Precision.AlmostEquals(Vector2.Zero, parent.DrawSize));
        }

        private class TestBox1 : Box
        {
            public override bool RemoveWhenNotAlive => false;
        }
    }
}
