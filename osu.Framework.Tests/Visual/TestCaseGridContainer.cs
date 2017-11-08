// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

            AddStep("Blank grid", () => loadGrid(0));
            AddStep("1-cell (auto)", () => loadGrid(1));
            AddStep("1-cell (absolute)", () => loadGrid(2));
            AddStep("1-cell (relative)", () => loadGrid(3));
            AddStep("1-cell (mixed)", () => loadGrid(4));
            AddStep("1-cell (mixed) 2", () => loadGrid(5));
            AddStep("3-cell row (auto)", () => loadGrid(6));
            AddStep("3-cell row (absolute", () => loadGrid(7));
            AddStep("3-cell row (relative)", () => loadGrid(8));
            AddStep("3-cell row (mixed)", () => loadGrid(9));
            AddStep("3-cell column (auto)", () => loadGrid(10));
            AddStep("3-cell column (absolute", () => loadGrid(11));
            AddStep("3-cell column (relative)", () => loadGrid(12));
            AddStep("3-cell column (mixed)", () => loadGrid(13));
            AddStep("3x3-cell (auto)", () => loadGrid(14));
            AddStep("3x3-cell (absolute", () => loadGrid(15));
            AddStep("3x3-cell (relative)", () => loadGrid(16));
            AddStep("3x3-cell (mixed)", () => loadGrid(17));
            AddStep("Separated", () => loadGrid(18));
            AddStep("Separated 2", () => loadGrid(19));
            AddStep("Nested grids", () => loadGrid(20));
        }

        private void loadGrid(int testCase)
        {
            grid.ClearInternal();
            grid.RowDimensions = grid.ColumnDimensions = new Dimension[] { };

            switch (testCase)
            {
                case 0:
                    break;
                case 1:
                    grid.Content = new Drawable[][] { new[] { new FillBox() } };
                    break;
                case 2:
                    grid.Content = new Drawable[][] { new[] { new FillBox() } };
                    grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(0) { Size = 100 } };
                    break;
                case 3:
                    grid.Content = new Drawable[][] { new[] { new FillBox() } };
                    grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(0) { Size = 0.5f, Relative = true } };
                    break;
                case 4:
                    grid.Content = new Drawable[][] { new[] { new FillBox() } };
                    grid.RowDimensions = new[] { new Dimension(0) { Size = 100 } };
                    grid.ColumnDimensions = new[] { new Dimension(0) { Size = 0.5f, Relative = true } };
                    break;
                case 5:
                    grid.Content = new Drawable[][] { new[] { new FillBox() } };
                    grid.RowDimensions = new[] { new Dimension(0) { Size = 0.5f, Relative = true } };
                    break;
                case 6:
                    grid.Content = new Drawable[][] { new[] { new FillBox(), new FillBox(), new FillBox() } };
                    break;
                case 7:
                    grid.Content = new Drawable[][] { new[] { new FillBox(), new FillBox(), new FillBox() } };
                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 100 },
                        new Dimension(2) { Size = 150 }
                    };
                    break;
                case 8:
                    grid.Content = new Drawable[][] { new[] { new FillBox(), new FillBox(), new FillBox() } };
                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 0.1f, Relative = true },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                        new Dimension(2) { Size = 0.3f, Relative = true }
                    };
                    break;
                case 9:
                    grid.Content = new Drawable[][] { new[] { new FillBox(), new FillBox(), new FillBox() } };
                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                    };
                    break;
                case 10:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox() },
                        new[] { new FillBox() },
                        new[] { new FillBox() }
                    };
                    break;
                case 11:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox() },
                        new[] { new FillBox() },
                        new[] { new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 100 },
                        new Dimension(2) { Size = 150 }
                    };
                    break;
                case 12:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox() },
                        new[] { new FillBox() },
                        new[] { new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 0.1f, Relative = true },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                        new Dimension(2) { Size = 0.3f, Relative = true }
                    };
                    break;
                case 13:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox() },
                        new[] { new FillBox() },
                        new[] { new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                    };
                    break;
                case 14:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() }
                    };
                    break;
                case 15:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 100 },
                        new Dimension(2) { Size = 150 }
                    };
                    break;
                case 16:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 0.1f, Relative = true },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                        new Dimension(2) { Size = 0.3f, Relative = true }
                    };
                    break;
                case 17:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() },
                        new[] { new FillBox(), new FillBox(), new FillBox() }
                    };

                    grid.RowDimensions = grid.ColumnDimensions = new[]
                    {
                        new Dimension(0) { Size = 50 },
                        new Dimension(1) { Size = 0.2f, Relative = true },
                    };
                    break;
                case 18:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), null, new FillBox() },
                        null,
                        new[] { new FillBox(), null, new FillBox() }
                    };
                    break;
                case 19:
                    grid.Content = new Drawable[][]
                    {
                        new[] { new FillBox(), null, new FillBox(), null },
                        null,
                        new[] { new FillBox(), null, new FillBox(), null },
                        null
                    };
                    break;
                case 20:
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
                                            Content = new Drawable[][]
                                            {
                                                new[] { new FillBox(), new FillBox() },
                                                new[] { new FillBox(), new FillBox() }
                                            }
                                        }
                                    }
                                }
                            },
                            new FillBox()
                        }
                    };
                    break;
            }
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
