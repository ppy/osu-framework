// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseGridContainer : TestCase
    {
        private readonly GridContainer grid;

        public TestCaseGridContainer()
        {
            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Masking = true,
                BorderColour = Color4.White,
                BorderThickness = 2,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    },
                    grid = new GridContainer { RelativeSizeAxes = Axes.Both }
                }
            });

            AddStep("Blank grid", reset);
            AddStep("1-cell (auto)", () =>
            {
                reset();
                grid.Content = new[] { new Drawable[] { new FillBox() } };
            });

            AddStep("1-cell (absolute)", () =>
            {
                reset();
                grid.Content = new[] { new Drawable[] { new FillBox() } };
                grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, 100) };
            });

            AddStep("1-cell (relative)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox() } };
                grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f) };
            });

            AddStep("1-cell (mixed)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox() } };
                grid.RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 100) };
                grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.5f) };
            });

            AddStep("1-cell (mixed) 2", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox() } };
                grid.RowDimensions = new [] { new Dimension(GridSizeMode.Relative, 0.5f) };
            });

            AddStep("3-cell row (auto)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox(), new FillBox(), new FillBox() } };
            });

            AddStep("3-cell row (absolute)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox(), new FillBox(), new FillBox() } };
                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Absolute, 100),
                    new Dimension(GridSizeMode.Absolute, 150)
                };
            });

            AddStep("3-cell row (relative)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox(), new FillBox(), new FillBox() } };
                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.1f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.3f)
                };
            });

            AddStep("3-cell row (mixed)", () =>
            {
                reset();
                grid.Content = new [] { new Drawable[] { new FillBox(), new FillBox(), new FillBox() } };
                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Relative, 0.2f)
                };
            });

            AddStep("3-cell column (auto)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() }
                };
            });

            AddStep("3-cell column (absolute)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Absolute, 100),
                    new Dimension(GridSizeMode.Absolute, 150)
                };
            });

            AddStep("3-cell column (relative)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.1f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.3f)
                };
            });

            AddStep("3-cell column (mixed)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() },
                    new Drawable[] { new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Relative, 0.2f)
                };
            });

            AddStep("3x3-cell (auto)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };
            });

            AddStep("3x3-cell (absolute)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Absolute, 100),
                    new Dimension(GridSizeMode.Absolute, 150)
                };
            });

            AddStep("3x3-cell (relative)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.1f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.3f)
                };
            });

            AddStep("3x3-cell (mixed)", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 50),
                    new Dimension(GridSizeMode.Relative, 0.2f)
                };
            });

            AddStep("Larger sides", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };

                grid.ColumnDimensions = grid.RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.4f),
                    new Dimension(),
                    new Dimension(GridSizeMode.Relative, 0.4f)
                };
            });

            AddStep("Separated", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), null, new FillBox() },
                    null,
                    new Drawable[] { new FillBox(), null, new FillBox() }
                };
            });

            AddStep("Separated 2", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), null, new FillBox(), null },
                    null,
                    new Drawable[] { new FillBox(), null, new FillBox(), null },
                    null
                };
            });

            AddStep("Nested grids", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[]
                    {
                        new FillBox(),
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Content = new[]
                            {
                                new Drawable[] { new FillBox(), new FillBox() },
                                new Drawable[]
                                {
                                    new FillBox(),
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Content = new[]
                                        {
                                            new Drawable[] { new FillBox(), new FillBox() },
                                            new Drawable[] { new FillBox(), new FillBox() }
                                        }
                                    }
                                }
                            }
                        },
                        new FillBox()
                    }
                };
            });

            AddStep("Auto size", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[] { new Box { Size = new Vector2(30) }, new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                };

                grid.RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension(GridSizeMode.Relative, 0.5f) };
                grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension(GridSizeMode.Relative, 0.5f) };
            });

            AddStep("Autosizing child", () =>
            {
                reset();
                grid.Content = new[]
                {
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Child = new Box { Size = new Vector2(100, 50) }
                        },
                        new FillBox()
                    }
                };

                grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize) };
            });
        }

        private void reset()
        {
            grid.ClearInternal();
            grid.RowDimensions = grid.ColumnDimensions = new Dimension[] { };
        }

        private class FillBox : Box
        {
            public FillBox()
            {
                RelativeSizeAxes = Axes.Both;
                Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1);
            }
        }
    }
}
