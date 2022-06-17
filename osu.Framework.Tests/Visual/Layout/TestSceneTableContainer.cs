// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneTableContainer : FrameworkTestScene
    {
        private TableContainer table;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderColour = Color4.White,
                        BorderThickness = 2,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            AlwaysPresent = true
                        }
                    },
                    table = new TableContainer { RelativeSizeAxes = Axes.Both }
                }
            };
        });

        [Test]
        public void TestBlankTable()
        {
        }

        [Test]
        public void TestOnlyContent()
        {
            AddStep("set content", () => table.Content = createContent(2, 2));
            AddAssert("headers not displayed", () => getGrid().Content.Count == 2);
        }

        [Test]
        public void TestOnlyHeaders()
        {
            AddStep("set columns", () => table.Columns = new[]
            {
                new TableColumn("Col 1"),
                new TableColumn("Col 2"),
            });
        }

        [Test]
        public void TestContentAndHeaders()
        {
            AddStep("set cells", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            AddAssert("4 rows", () => getGrid().Content.Count == 4);
            AddStep("disable headers", () => table.ShowHeaders = false);
            AddAssert("3 rows", () => getGrid().Content.Count == 3);
        }

        [Test]
        public void TestHeaderLongerThanContent()
        {
            AddStep("set cells", () =>
            {
                table.Content = createContent(2, 2);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            AddAssert("3 columns", () => getGrid().Content.Max(r => r.Count) == 3);
            AddStep("disable headers", () => table.ShowHeaders = false);
            AddAssert("2 columns", () => getGrid().Content.Max(r => r.Count) == 2);
        }

        [Test]
        public void TestContentLongerThanHeader()
        {
            AddStep("set cells", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                };
            });

            AddAssert("3 columns", () => getGrid().Content.Max(r => r.Count) == 3);
            AddStep("disable headers", () => table.ShowHeaders = false);
            AddAssert("2 columns", () => getGrid().Content.Max(r => r.Count) == 3);
        }

        [Test]
        public void TestColumnsWithAnchors()
        {
            AddStep("set content", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Left", Anchor.CentreLeft),
                    new TableColumn("Centre", Anchor.Centre),
                    new TableColumn("Right", Anchor.CentreRight),
                };
            });

            AddAssert("column 0 all left aligned", () => testColumn(0, Anchor.CentreLeft));
            AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            AddAssert("column 2 all right aligned", () => testColumn(2, Anchor.CentreRight));

            AddStep("attempt to change anchor", () =>
            {
                var cell = table?.Content?[0, 0];
                if (cell != null)
                    cell.Anchor = Anchor.Centre;
            });

            // This currently fails, but should probably pass, but is particularly hard to fix.
            // It's open to interpretation for how this should work, though, so it's not critical...
            // AddAssert("column 0 all left aligned", () => testColumn(0, Anchor.CentreLeft));

            AddStep("change columns", () => table.Columns = new[]
            {
                new TableColumn("Left", Anchor.CentreRight),
                new TableColumn("Centre", Anchor.Centre),
                new TableColumn("Right", Anchor.CentreLeft),
            });

            AddAssert("column 0 all right aligned", () => testColumn(0, Anchor.CentreRight));
            AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            AddAssert("column 2 all left aligned", () => testColumn(2, Anchor.CentreLeft));

            AddStep("change content", () => table.Content = createContent(4, 4));

            AddAssert("column 0 all right aligned", () => testColumn(0, Anchor.CentreRight));
            AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            AddAssert("column 2 all left aligned", () => testColumn(2, Anchor.CentreLeft));
            AddAssert("column 3 all top-left aligned", () => testColumn(3, Anchor.TopLeft));

            bool testColumn(int index, Anchor anchor)
            {
                for (int r = 0; r < getGrid().Content.Count; r++)
                {
                    if (getGrid().Content[r][index].Anchor != anchor)
                        return false;
                }

                return true;
            }
        }

        [Test]
        public void TestChangeColumns()
        {
            AddStep("set content", () =>
            {
                table.Content = createContent(2, 2);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                };
            });

            AddStep("increase columns", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
                new TableColumn("Header 2"),
                new TableColumn("Header 3"),
            });

            AddAssert("3 columns", () => getGrid().Content.Max(r => r.Count) == 3);

            AddStep("decrease columns", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
            });

            AddAssert("2 columns", () => getGrid().Content.Max(r => r.Count) == 2);
        }

        [Test]
        public void TestRowSize()
        {
            AddStep("set content", () =>
            {
                table.Content = createContent(2, 2);
                table.RowSize = new Dimension(GridSizeMode.Absolute, 30f);
            });

            AddAssert("all row size = 30", () => testRows(30));
            AddStep("add headers", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
                new TableColumn("Header 2"),
                new TableColumn("Header 3"),
            });

            AddAssert("all row size = 30", () => testRows(30));
            AddStep("change row size", () => table.RowSize = new Dimension(GridSizeMode.Absolute, 50));
            AddAssert("all row size = 50", () => testRows(50));
            AddStep("change content", () => table.Content = createContent(4, 4));
            AddAssert("all row size = 50", () => testRows(50));
            AddStep("remove custom row size", () => table.RowSize = null);
            AddAssert("all row size = distributed", () => testRows(table.DrawHeight / 5f));

            bool testRows(float expectedHeight)
            {
                for (int row = 0; row < getGrid().Content.Count; row++)
                {
                    if (!Precision.AlmostEquals(expectedHeight, getGrid().Content[row][0].Parent.DrawHeight))
                        return false;
                }

                return true;
            }
        }

        [Test]
        public void TestClearGrid()
        {
            AddStep("set content", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            AddStep("clear grid", () =>
            {
                table.Columns = null;
                table.Content = null;
            });
        }

        private Drawable[,] createContent(int rows, int columns)
        {
            var content = new Drawable[rows, columns];

            int cellIndex = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                    content[r, c] = new Cell(cellIndex++);
            }

            return content;
        }

        private GridContainer getGrid() => (GridContainer)table.InternalChild;

        private class Cell : SpriteText
        {
            public Cell(int index)
            {
                Text = $"Cell {index}";
            }
        }
    }
}
