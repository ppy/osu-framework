// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
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
            Steps.AddStep("set content", () => table.Content = createContent(2, 2));
            Steps.AddAssert("headers not displayed", () => getGrid().Content.Length == 2);
        }

        [Test]
        public void TestOnlyHeaders()
        {
            Steps.AddStep("set columns", () => table.Columns = new[]
            {
                new TableColumn("Col 1"),
                new TableColumn("Col 2"),
            });
        }

        [Test]
        public void TestContentAndHeaders()
        {
            Steps.AddStep("set cells", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            Steps.AddAssert("4 rows", () => getGrid().Content.Length == 4);
            Steps.AddStep("disable headers", () => table.ShowHeaders = false);
            Steps.AddAssert("3 rows", () => getGrid().Content.Length == 3);
        }

        [Test]
        public void TestHeaderLongerThanContent()
        {
            Steps.AddStep("set cells", () =>
            {
                table.Content = createContent(2, 2);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            Steps.AddAssert("3 columns", () => getGrid().Content.Max(r => r.Length) == 3);
            Steps.AddStep("disable headers", () => table.ShowHeaders = false);
            Steps.AddAssert("2 columns", () => getGrid().Content.Max(r => r.Length) == 2);
        }

        [Test]
        public void TestContentLongerThanHeader()
        {
            Steps.AddStep("set cells", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                };
            });

            Steps.AddAssert("3 columns", () => getGrid().Content.Max(r => r.Length) == 3);
            Steps.AddStep("disable headers", () => table.ShowHeaders = false);
            Steps.AddAssert("2 columns", () => getGrid().Content.Max(r => r.Length) == 3);
        }

        [Test]
        public void TestColumnsWithAnchors()
        {
            Steps.AddStep("set content", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Left", Anchor.CentreLeft),
                    new TableColumn("Centre", Anchor.Centre),
                    new TableColumn("Right", Anchor.CentreRight),
                };
            });

            Steps.AddAssert("column 0 all left aligned", () => testColumn(0, Anchor.CentreLeft));
            Steps.AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            Steps.AddAssert("column 2 all right aligned", () => testColumn(2, Anchor.CentreRight));

            Steps.AddStep("attempt to change anchor", () =>
            {
                var cell = table?.Content?[0, 0];
                if (cell != null)
                    cell.Anchor = Anchor.Centre;
            });

            // This currently fails, but should probably pass, but is particularly hard to fix.
            // It's open to interpretation for how this should work, though, so it's not critical...
            // Steps.AddAssert("column 0 all left aligned", () => testColumn(0, Anchor.CentreLeft));

            Steps.AddStep("change columns", () => table.Columns = new[]
            {
                new TableColumn("Left", Anchor.CentreRight),
                new TableColumn("Centre", Anchor.Centre),
                new TableColumn("Right", Anchor.CentreLeft),
            });

            Steps.AddAssert("column 0 all right aligned", () => testColumn(0, Anchor.CentreRight));
            Steps.AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            Steps.AddAssert("column 2 all left aligned", () => testColumn(2, Anchor.CentreLeft));

            Steps.AddStep("change content", () => table.Content = createContent(4, 4));

            Steps.AddAssert("column 0 all right aligned", () => testColumn(0, Anchor.CentreRight));
            Steps.AddAssert("column 1 all centre aligned", () => testColumn(1, Anchor.Centre));
            Steps.AddAssert("column 2 all left aligned", () => testColumn(2, Anchor.CentreLeft));
            Steps.AddAssert("column 3 all top-left aligned", () => testColumn(3, Anchor.TopLeft));

            bool testColumn(int index, Anchor anchor)
            {
                for (int r = 0; r < getGrid().Content.Length; r++)
                    if (getGrid().Content[r][index].Anchor != anchor)
                        return false;

                return true;
            }
        }

        [Test]
        public void TestChangeColumns()
        {
            Steps.AddStep("set content", () =>
            {
                table.Content = createContent(2, 2);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                };
            });

            Steps.AddStep("increase columns", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
                new TableColumn("Header 2"),
                new TableColumn("Header 3"),
            });

            Steps.AddAssert("3 columns", () => getGrid().Content.Max(r => r.Length) == 3);

            Steps.AddStep("decrease columns", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
            });

            Steps.AddAssert("2 columns", () => getGrid().Content.Max(r => r.Length) == 2);
        }

        [Test]
        public void TestRowSize()
        {
            Steps.AddStep("set content", () =>
            {
                table.Content = createContent(2, 2);
                table.RowSize = new Dimension(GridSizeMode.Absolute, 30f);
            });

            Steps.AddAssert("all row size = 30", () => testRows(30));
            Steps.AddStep("add headers", () => table.Columns = new[]
            {
                new TableColumn("Header 1"),
                new TableColumn("Header 2"),
                new TableColumn("Header 3"),
            });

            Steps.AddAssert("all row size = 30", () => testRows(30));
            Steps.AddStep("change row size", () => table.RowSize = new Dimension(GridSizeMode.Absolute, 50));
            Steps.AddAssert("all row size = 50", () => testRows(50));
            Steps.AddStep("change content", () => table.Content = createContent(4, 4));
            Steps.AddAssert("all row size = 50", () => testRows(50));
            Steps.AddStep("remove custom row size", () => table.RowSize = null);
            Steps.AddAssert("all row size = distributed", () => testRows(table.DrawHeight / 5f));

            bool testRows(float expectedHeight)
            {
                for (int row = 0; row < getGrid().Content.Length; row++)
                    if (!Precision.AlmostEquals(expectedHeight, getGrid().Content[row][0].Parent.DrawHeight))
                        return false;

                return true;
            }
        }

        [Test]
        public void TestClearGrid()
        {
            Steps.AddStep("set content", () =>
            {
                table.Content = createContent(3, 3);
                table.Columns = new[]
                {
                    new TableColumn("Header 1"),
                    new TableColumn("Header 2"),
                    new TableColumn("Header 3"),
                };
            });

            Steps.AddStep("clear grid", () =>
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
            for (int c = 0; c < columns; c++)
                content[r, c] = new Cell(cellIndex++);

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
