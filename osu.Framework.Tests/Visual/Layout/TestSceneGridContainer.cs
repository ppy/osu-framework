// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneGridContainer : FrameworkTestScene
    {
        private Container gridParent;
        private GridContainer grid;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = gridParent = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Children = new Drawable[]
                {
                    grid = new GridContainer { RelativeSizeAxes = Axes.Both },
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
                    }
                }
            };
        });

        [TestCase(false)]
        [TestCase(true)]
        public void TestAutoSizeDoesNotConsiderRelativeSizeChildren(bool row)
        {
            Box relativeBox = null;
            Box absoluteBox = null;

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    relativeBox = new FillBox { RelativeSizeAxes = Axes.Both },
                    absoluteBox = new FillBox
                    {
                        RelativeSizeAxes = Axes.None,
                        Size = new Vector2(100)
                    }
                }
            }, new[] { new Dimension(GridSizeMode.AutoSize) }, row);

            AddStep("resize absolute box", () => absoluteBox.Size = new Vector2(50));
            AddAssert("relative box has length 50", () => Precision.AlmostEquals(row ? relativeBox.DrawHeight : relativeBox.DrawWidth, 50, 1));
        }

        [Test]
        public void TestBlankGrid()
        {
        }

        [Test]
        public void TestSingleCellDistributedXy()
        {
            FillBox box = null;
            AddStep("set content", () => grid.Content = new[] { new Drawable[] { box = new FillBox() }, });
            AddAssert("box is same size as grid", () => Precision.AlmostEquals(box.DrawSize, grid.DrawSize));
        }

        [Test]
        public void TestSingleCellAbsoluteXy()
        {
            const float size = 100;

            FillBox box = null;
            AddStep("set content", () =>
            {
                grid.Content = new[] { new Drawable[] { box = new FillBox() }, };
                grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, size) };
            });

            AddAssert("box has expected size", () => Precision.AlmostEquals(box.DrawSize, new Vector2(size)));
        }

        [Test]
        public void TestSingleCellRelativeXy()
        {
            const float size = 0.5f;

            FillBox box = null;
            AddStep("set content", () =>
            {
                grid.Content = new[] { new Drawable[] { box = new FillBox() }, };
                grid.RowDimensions = grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, size) };
            });

            AddAssert("box has expected size", () => Precision.AlmostEquals(box.DrawSize, grid.DrawSize * new Vector2(size)));
        }

        [Test]
        public void TestSingleCellRelativeXAbsoluteY()
        {
            const float absolute_height = 100;
            const float relative_width = 0.5f;

            FillBox box = null;
            AddStep("set content", () =>
            {
                grid.Content = new[] { new Drawable[] { box = new FillBox() }, };
                grid.RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, absolute_height) };
                grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, relative_width) };
            });

            AddAssert("box has expected width", () => Precision.AlmostEquals(box.DrawWidth, grid.DrawWidth * relative_width));
            AddAssert("box has expected height", () => Precision.AlmostEquals(box.DrawHeight, absolute_height));
        }

        [Test]
        public void TestSingleCellDistributedXRelativeY()
        {
            const float height = 0.5f;

            FillBox box = null;
            AddStep("set content", () =>
            {
                grid.Content = new[] { new Drawable[] { box = new FillBox() }, };
                grid.RowDimensions = new[] { new Dimension(GridSizeMode.Relative, height) };
            });

            AddAssert("box has expected width", () => Precision.AlmostEquals(box.DrawWidth, grid.DrawWidth));
            AddAssert("box has expected height", () => Precision.AlmostEquals(box.DrawHeight, grid.DrawHeight * height));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Test3CellRowOrColumnDistributedXy(bool row)
        {
            FillBox[] boxes = new FillBox[3];

            setSingleDimensionContent(() => new[]
            {
                new Drawable[] { boxes[0] = new FillBox(), boxes[1] = new FillBox(), boxes[2] = new FillBox() }
            }, row: row);

            for (int i = 0; i < 3; i++)
            {
                int local = i;

                if (row)
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(grid.DrawWidth / 3f, grid.DrawHeight)));
                else
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(grid.DrawWidth, grid.DrawHeight / 3f)));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Test3CellRowOrColumnDistributedXyAbsoluteYx(bool row)
        {
            float[] sizes = { 50f, 100f, 75f };
            var boxes = new FillBox[3];

            setSingleDimensionContent(() => new[]
            {
                new Drawable[] { boxes[0] = new FillBox() },
                new Drawable[] { boxes[1] = new FillBox() },
                new Drawable[] { boxes[2] = new FillBox() },
            }, new[]
            {
                new Dimension(GridSizeMode.Absolute, sizes[0]),
                new Dimension(GridSizeMode.Absolute, sizes[1]),
                new Dimension(GridSizeMode.Absolute, sizes[2])
            }, row);

            for (int i = 0; i < 3; i++)
            {
                int local = i;

                if (row)
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(grid.DrawWidth, sizes[local])));
                else
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(sizes[local], grid.DrawHeight)));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Test3CellRowOrColumnDistributedXyRelativeYx(bool row)
        {
            float[] sizes = { 0.2f, 0.4f, 0.2f };
            var boxes = new FillBox[3];

            setSingleDimensionContent(() => new[]
            {
                new Drawable[] { boxes[0] = new FillBox() },
                new Drawable[] { boxes[1] = new FillBox() },
                new Drawable[] { boxes[2] = new FillBox() },
            }, new[]
            {
                new Dimension(GridSizeMode.Relative, sizes[0]),
                new Dimension(GridSizeMode.Relative, sizes[1]),
                new Dimension(GridSizeMode.Relative, sizes[2])
            }, row);

            for (int i = 0; i < 3; i++)
            {
                int local = i;

                if (row)
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(grid.DrawWidth, sizes[local] * grid.DrawHeight)));
                else
                    AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, new Vector2(sizes[local] * grid.DrawWidth, grid.DrawHeight)));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Test3CellRowOrColumnDistributedXyMixedYx(bool row)
        {
            float[] sizes = { 0.2f, 75f };
            var boxes = new FillBox[3];

            setSingleDimensionContent(() => new[]
            {
                new Drawable[] { boxes[0] = new FillBox() },
                new Drawable[] { boxes[1] = new FillBox() },
                new Drawable[] { boxes[2] = new FillBox() },
            }, new[]
            {
                new Dimension(GridSizeMode.Relative, sizes[0]),
                new Dimension(GridSizeMode.Absolute, sizes[1]),
                new Dimension(),
            }, row);

            if (row)
            {
                AddAssert("box 0 has correct size", () => Precision.AlmostEquals(boxes[0].DrawSize, new Vector2(grid.DrawWidth, sizes[0] * grid.DrawHeight)));
                AddAssert("box 1 has correct size", () => Precision.AlmostEquals(boxes[1].DrawSize, new Vector2(grid.DrawWidth, sizes[1])));
                AddAssert("box 2 has correct size", () => Precision.AlmostEquals(boxes[2].DrawSize, new Vector2(grid.DrawWidth, grid.DrawHeight - boxes[0].DrawHeight - boxes[1].DrawHeight)));
            }
            else
            {
                AddAssert("box 0 has correct size", () => Precision.AlmostEquals(boxes[0].DrawSize, new Vector2(sizes[0] * grid.DrawWidth, grid.DrawHeight)));
                AddAssert("box 1 has correct size", () => Precision.AlmostEquals(boxes[1].DrawSize, new Vector2(sizes[1], grid.DrawHeight)));
                AddAssert("box 2 has correct size", () => Precision.AlmostEquals(boxes[2].DrawSize, new Vector2(grid.DrawWidth - boxes[0].DrawWidth - boxes[1].DrawWidth, grid.DrawHeight)));
            }
        }

        [Test]
        public void Test3X3GridDistributedXy()
        {
            var boxes = new FillBox[9];

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[] { boxes[0] = new FillBox(), boxes[1] = new FillBox(), boxes[2] = new FillBox() },
                    new Drawable[] { boxes[3] = new FillBox(), boxes[4] = new FillBox(), boxes[5] = new FillBox() },
                    new Drawable[] { boxes[6] = new FillBox(), boxes[7] = new FillBox(), boxes[8] = new FillBox() }
                };
            });

            for (int i = 0; i < 9; i++)
            {
                int local = i;
                AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(grid.DrawSize / 3f, boxes[local].DrawSize));
            }
        }

        [Test]
        public void Test3X3GridAbsoluteXy()
        {
            var boxes = new FillBox[9];

            var dimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, 50),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Absolute, 75)
            };

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[] { boxes[0] = new FillBox(), boxes[1] = new FillBox(), boxes[2] = new FillBox() },
                    new Drawable[] { boxes[3] = new FillBox(), boxes[4] = new FillBox(), boxes[5] = new FillBox() },
                    new Drawable[] { boxes[6] = new FillBox(), boxes[7] = new FillBox(), boxes[8] = new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = dimensions;
            });

            for (int i = 0; i < 9; i++)
            {
                int local = i;
                AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(new Vector2(dimensions[local % 3].Size, dimensions[local / 3].Size), boxes[local].DrawSize));
            }
        }

        [Test]
        public void Test3X3GridRelativeXy()
        {
            var boxes = new FillBox[9];

            var dimensions = new[]
            {
                new Dimension(GridSizeMode.Relative, 0.2f),
                new Dimension(GridSizeMode.Relative, 0.4f),
                new Dimension(GridSizeMode.Relative, 0.2f)
            };

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[] { boxes[0] = new FillBox(), boxes[1] = new FillBox(), boxes[2] = new FillBox() },
                    new Drawable[] { boxes[3] = new FillBox(), boxes[4] = new FillBox(), boxes[5] = new FillBox() },
                    new Drawable[] { boxes[6] = new FillBox(), boxes[7] = new FillBox(), boxes[8] = new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = dimensions;
            });

            for (int i = 0; i < 9; i++)
            {
                int local = i;
                AddAssert($"box {local} has correct size",
                    () => Precision.AlmostEquals(new Vector2(dimensions[local % 3].Size * grid.DrawWidth, dimensions[local / 3].Size * grid.DrawHeight), boxes[local].DrawSize));
            }
        }

        [Test]
        public void Test3X3GridMixedXy()
        {
            var boxes = new FillBox[9];

            var dimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, 50),
                new Dimension(GridSizeMode.Relative, 0.2f)
            };

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[] { boxes[0] = new FillBox(), boxes[1] = new FillBox(), boxes[2] = new FillBox() },
                    new Drawable[] { boxes[3] = new FillBox(), boxes[4] = new FillBox(), boxes[5] = new FillBox() },
                    new Drawable[] { boxes[6] = new FillBox(), boxes[7] = new FillBox(), boxes[8] = new FillBox() }
                };

                grid.RowDimensions = grid.ColumnDimensions = dimensions;
            });

            // Row 1
            AddAssert("box 0 has correct size", () => Precision.AlmostEquals(boxes[0].DrawSize, new Vector2(dimensions[0].Size, dimensions[0].Size)));
            AddAssert("box 1 has correct size", () => Precision.AlmostEquals(boxes[1].DrawSize, new Vector2(grid.DrawWidth * dimensions[1].Size, dimensions[0].Size)));
            AddAssert("box 2 has correct size", () => Precision.AlmostEquals(boxes[2].DrawSize, new Vector2(grid.DrawWidth - boxes[0].DrawWidth - boxes[1].DrawWidth, dimensions[0].Size)));

            // Row 2
            AddAssert("box 3 has correct size", () => Precision.AlmostEquals(boxes[3].DrawSize, new Vector2(dimensions[0].Size, grid.DrawHeight * dimensions[1].Size)));
            AddAssert("box 4 has correct size", () => Precision.AlmostEquals(boxes[4].DrawSize, new Vector2(grid.DrawWidth * dimensions[1].Size, grid.DrawHeight * dimensions[1].Size)));
            AddAssert("box 5 has correct size",
                () => Precision.AlmostEquals(boxes[5].DrawSize, new Vector2(grid.DrawWidth - boxes[0].DrawWidth - boxes[1].DrawWidth, grid.DrawHeight * dimensions[1].Size)));

            // Row 3
            AddAssert("box 6 has correct size", () => Precision.AlmostEquals(boxes[6].DrawSize, new Vector2(dimensions[0].Size, grid.DrawHeight - boxes[3].DrawHeight - boxes[0].DrawHeight)));
            AddAssert("box 7 has correct size",
                () => Precision.AlmostEquals(boxes[7].DrawSize, new Vector2(grid.DrawWidth * dimensions[1].Size, grid.DrawHeight - boxes[4].DrawHeight - boxes[1].DrawHeight)));
            AddAssert("box 8 has correct size",
                () => Precision.AlmostEquals(boxes[8].DrawSize, new Vector2(grid.DrawWidth - boxes[0].DrawWidth - boxes[1].DrawWidth, grid.DrawHeight - boxes[5].DrawHeight - boxes[2].DrawHeight)));
        }

        [Test]
        public void TestGridWithNullRowsAndColumns()
        {
            var boxes = new FillBox[4];

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[] { boxes[0] = new FillBox(), null, boxes[1] = new FillBox(), null },
                    null,
                    new Drawable[] { boxes[2] = new FillBox(), null, boxes[3] = new FillBox(), null },
                    null
                };
            });

            AddAssert("two extra rows and columns", () =>
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!Precision.AlmostEquals(boxes[i].DrawSize, grid.DrawSize / 4))
                        return false;
                }

                return true;
            });
        }

        [Test]
        public void TestNestedGrids()
        {
            var boxes = new FillBox[4];

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Content = new[]
                            {
                                new Drawable[] { boxes[0] = new FillBox(), new FillBox(), },
                                new Drawable[] { new FillBox(), boxes[1] = new FillBox(), },
                            }
                        },
                        new FillBox(),
                    },
                    new Drawable[]
                    {
                        new FillBox(),
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Content = new[]
                            {
                                new Drawable[] { boxes[2] = new FillBox(), new FillBox(), },
                                new Drawable[] { new FillBox(), boxes[3] = new FillBox(), },
                            }
                        }
                    }
                };
            });

            for (int i = 0; i < 4; i++)
            {
                int local = i;
                AddAssert($"box {local} has correct size", () => Precision.AlmostEquals(boxes[local].DrawSize, grid.DrawSize / 4));
            }
        }

        [Test]
        public void TestGridWithAutoSizingCells()
        {
            FillBox fillBox = null;
            var autoSizingChildren = new Drawable[2];

            AddStep("set content", () =>
            {
                grid.Content = new[]
                {
                    new[]
                    {
                        autoSizingChildren[0] = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(50, 10)
                        },
                        fillBox = new FillBox(),
                    },
                    new[]
                    {
                        null,
                        autoSizingChildren[1] = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(50, 10)
                        },
                    },
                };

                grid.ColumnDimensions = new[] { new Dimension(GridSizeMode.AutoSize) };
                grid.RowDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize),
                };
            });

            AddAssert("fill box has correct size", () => Precision.AlmostEquals(fillBox.DrawSize, new Vector2(grid.DrawWidth - 50, grid.DrawHeight - 10)));
            AddStep("rotate boxes", () => autoSizingChildren.ForEach(c => c.RotateTo(90)));
            AddAssert("fill box has resized correctly", () => Precision.AlmostEquals(fillBox.DrawSize, new Vector2(grid.DrawWidth - 10, grid.DrawHeight - 50)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDimensionsWithMaximumSize(bool row)
        {
            var boxes = new FillBox[8];

            var dimensions = new[]
            {
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, maxSize: 100),
                new Dimension(),
                new Dimension(GridSizeMode.Distributed, maxSize: 50),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, maxSize: 80),
                new Dimension(GridSizeMode.Distributed, maxSize: 150)
            };

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    boxes[0] = new FillBox(),
                    boxes[1] = new FillBox(),
                    boxes[2] = new FillBox(),
                    boxes[3] = new FillBox(),
                    boxes[4] = new FillBox(),
                    boxes[5] = new FillBox(),
                    boxes[6] = new FillBox(),
                    boxes[7] = new FillBox()
                },
            }.Invert(), dimensions, row);

            checkClampedSizes(row, boxes, dimensions);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDimensionsWithMinimumSize(bool row)
        {
            var boxes = new FillBox[8];

            var dimensions = new[]
            {
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, minSize: 100),
                new Dimension(),
                new Dimension(GridSizeMode.Distributed, minSize: 50),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, minSize: 80),
                new Dimension(GridSizeMode.Distributed, minSize: 150)
            };

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    boxes[0] = new FillBox(),
                    boxes[1] = new FillBox(),
                    boxes[2] = new FillBox(),
                    boxes[3] = new FillBox(),
                    boxes[4] = new FillBox(),
                    boxes[5] = new FillBox(),
                    boxes[6] = new FillBox(),
                    boxes[7] = new FillBox()
                },
            }.Invert(), dimensions, row);

            checkClampedSizes(row, boxes, dimensions);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDimensionsWithMinimumAndMaximumSize(bool row)
        {
            var boxes = new FillBox[8];

            var dimensions = new[]
            {
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, minSize: 100),
                new Dimension(),
                new Dimension(GridSizeMode.Distributed, maxSize: 50),
                new Dimension(GridSizeMode.Absolute, 100),
                new Dimension(GridSizeMode.Distributed, minSize: 80),
                new Dimension(GridSizeMode.Distributed, maxSize: 150)
            };

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    boxes[0] = new FillBox(),
                    boxes[1] = new FillBox(),
                    boxes[2] = new FillBox(),
                    boxes[3] = new FillBox(),
                    boxes[4] = new FillBox(),
                    boxes[5] = new FillBox(),
                    boxes[6] = new FillBox(),
                    boxes[7] = new FillBox()
                },
            }.Invert(), dimensions, row);

            checkClampedSizes(row, boxes, dimensions);
        }

        [Test]
        public void TestCombinedMinimumAndMaximumSize()
        {
            AddStep("set content", () =>
            {
                gridParent.Masking = false;
                gridParent.RelativeSizeAxes = Axes.Y;
                gridParent.Width = 420;

                grid.Content = new[]
                {
                    new Drawable[]
                    {
                        new FillBox(),
                        new FillBox(),
                        new FillBox(),
                    },
                };

                grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Distributed, minSize: 180),
                    new Dimension(GridSizeMode.Distributed, minSize: 50, maxSize: 70),
                    new Dimension(GridSizeMode.Distributed, minSize: 40, maxSize: 70),
                };
            });

            AddAssert("content spans grid size", () => Precision.AlmostEquals(grid.DrawWidth, grid.Content[0].Sum(d => d.DrawWidth)));
        }

        [Test]
        public void TestCombinedMinimumAndMaximumSize2()
        {
            AddStep("set content", () =>
            {
                gridParent.Masking = false;
                gridParent.RelativeSizeAxes = Axes.Y;
                gridParent.Width = 230;

                grid.Content = new[]
                {
                    new Drawable[]
                    {
                        new FillBox(),
                        new FillBox(),
                    },
                };

                grid.ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Distributed, minSize: 180),
                    new Dimension(GridSizeMode.Distributed, minSize: 40, maxSize: 70),
                };
            });

            AddAssert("content spans grid size", () => Precision.AlmostEquals(grid.DrawWidth, grid.Content[0].Sum(d => d.DrawWidth)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestAutoSizedCellsWithTransparentContent(bool alwaysPresent)
        {
            AddStep("set content", () =>
            {
                grid.RowDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize)
                };
                grid.ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                };
                grid.Content = new[]
                {
                    new Drawable[] { new FillBox(), transparentBox(alwaysPresent), new FillBox() },
                    new Drawable[] { new FillBox(), transparentBox(alwaysPresent), new FillBox() },
                    new Drawable[] { transparentBox(alwaysPresent), transparentBox(alwaysPresent), transparentBox(alwaysPresent) }
                };
            });

            float desiredTransparentBoxSize = alwaysPresent ? 50 : 0;
            AddAssert("non-transparent fill boxes have correct size", () =>
                grid.Content
                    .SelectMany(row => row)
                    .Where(box => box.Alpha > 0)
                    .All(box => Precision.AlmostEquals(box.DrawWidth, (grid.DrawWidth - desiredTransparentBoxSize) / 2)
                                && Precision.AlmostEquals(box.DrawHeight, (grid.DrawHeight - desiredTransparentBoxSize) / 2)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestAutoSizedRowOrColumnWithTransparentContent(bool row)
        {
            var boxes = new FillBox[5];

            var dimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, 100f),
                new Dimension(),
                new Dimension(GridSizeMode.AutoSize),
                new Dimension(GridSizeMode.Relative, 0.2f),
                new Dimension()
            };

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    boxes[0] = new FillBox(),
                    boxes[1] = new FillBox(),
                    boxes[2] = transparentBox(false),
                    boxes[3] = new FillBox(),
                    boxes[4] = new FillBox()
                }
            }.Invert(), dimensions, row);

            AddAssert("box 0 has correct size", () => Precision.AlmostEquals(getDimension(boxes[0], row), 100f));
            AddAssert("box 1 has correct size", () =>
                Precision.AlmostEquals(getDimension(boxes[1], row), (getDimension(grid, row) * 0.8f - 100f) / 2));
            AddAssert("box 3 has correct size", () => Precision.AlmostEquals(getDimension(boxes[3], row), getDimension(grid, row) * 0.2f));
            AddAssert("box 4 has correct size", () =>
                Precision.AlmostEquals(getDimension(boxes[4], row), (getDimension(grid, row) * 0.8f - 100f) / 2));
        }

        private FillBox transparentBox(bool alwaysPresent) => new FillBox
        {
            Alpha = 0,
            AlwaysPresent = alwaysPresent,
            RelativeSizeAxes = Axes.None,
            Size = new Vector2(50)
        };

        [TestCase(true)]
        [TestCase(false)]
        public void TestAutoSizedRowOrColumnWithDelayedLifetimeContent(bool row)
        {
            var boxes = new FillBox[3];

            var dimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, 75f),
                new Dimension(GridSizeMode.AutoSize),
                new Dimension()
            };

            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    boxes[0] = new FillBox(),
                    boxes[1] = new FillBox
                    {
                        RelativeSizeAxes = Axes.None,
                        LifetimeStart = double.MaxValue,
                        Size = new Vector2(50)
                    },
                    boxes[2] = new FillBox()
                }
            }.Invert(), dimensions, row);

            AddAssert("box 0 has correct size", () => Precision.AlmostEquals(getDimension(boxes[0], row), 75f));
            AddAssert("box 2 has correct size", () => Precision.AlmostEquals(getDimension(boxes[2], row), getDimension(grid, row) - 75f));

            AddStep("make box 1 alive", () => boxes[1].LifetimeStart = Time.Current);
            AddUntilStep("wait for alive", () => boxes[1].IsAlive);

            AddAssert("box 0 has correct size", () => Precision.AlmostEquals(getDimension(boxes[0], row), 75f));
            AddAssert("box 2 has correct size", () => Precision.AlmostEquals(getDimension(boxes[2], row), getDimension(grid, row) - 125f));
        }

        private bool gridContentChangeEventWasFired;

        [Test]
        public void TestSetContentByIndex()
        {
            setSingleDimensionContent(() => new[]
            {
                new Drawable[]
                {
                    new FillBox(),
                    new FillBox()
                },
                new Drawable[]
                {
                    new FillBox(),
                    new FillBox()
                }
            });

            AddStep("Subscribe to event", () => grid.Content.ArrayElementChanged += () => gridContentChangeEventWasFired = true);

            AddStep("Replace bottom right box with a SpriteText", () =>
            {
                gridContentChangeEventWasFired = false;
                grid.Content[1][1] = new SpriteText { Text = "test" };
            });
            assertContentChangeEventWasFired();
            AddAssert("[1][1] cell contains a SpriteText", () => grid.Content[1][1].GetType() == typeof(SpriteText));

            AddStep("Replace top line with [SpriteText][null]", () =>
            {
                gridContentChangeEventWasFired = false;
                grid.Content[0] = new Drawable[] { new SpriteText { Text = "test" }, null };
            });
            assertContentChangeEventWasFired();
            AddAssert("[0][0] cell contains a SpriteText", () => grid.Content[0][0].GetType() == typeof(SpriteText));
            AddAssert("[0][1] cell contains null", () => grid.Content[0][1] == null);

            void assertContentChangeEventWasFired() => AddAssert("Content change event was fired", () => gridContentChangeEventWasFired);
        }

        /// <summary>
        /// Returns drawable dimension along desired axis.
        /// </summary>
        private float getDimension(Drawable drawable, bool row) => row ? drawable.DrawHeight : drawable.DrawWidth;

        private void checkClampedSizes(bool row, FillBox[] boxes, Dimension[] dimensions)
        {
            AddAssert("sizes not over/underflowed", () =>
            {
                for (int i = 0; i < 8; i++)
                {
                    if (dimensions[i].Mode != GridSizeMode.Distributed)
                        continue;

                    if (row && (boxes[i].DrawHeight > dimensions[i].MaxSize || boxes[i].DrawHeight < dimensions[i].MinSize))
                        return false;

                    if (!row && (boxes[i].DrawWidth > dimensions[i].MaxSize || boxes[i].DrawWidth < dimensions[i].MinSize))
                        return false;
                }

                return true;
            });

            AddAssert("column span total length", () =>
            {
                float expectedSize = row ? grid.DrawHeight : grid.DrawWidth;
                float totalSize = row ? boxes.Sum(b => b.DrawHeight) : boxes.Sum(b => b.DrawWidth);

                // Allowed to exceed the length of the columns due to absolute sizing
                return totalSize >= expectedSize;
            });
        }

        private void setSingleDimensionContent(Func<Drawable[][]> contentFunc, Dimension[] dimensions = null, bool row = false) => AddStep("set content", () =>
        {
            var content = contentFunc();

            if (!row)
                content = content.Invert();

            grid.Content = content;

            if (dimensions == null)
                return;

            if (row)
                grid.RowDimensions = dimensions;
            else
                grid.ColumnDimensions = dimensions;
        });

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
