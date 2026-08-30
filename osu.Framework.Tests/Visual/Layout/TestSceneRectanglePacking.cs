// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using System.Collections.Generic;
using osuTK;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;
using osu.Framework.Utils;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Utils.RectanglePacking;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Visual.Layout
{
    public partial class TestSceneRectanglePacking : FrameworkTestScene
    {
        private const int size = 170;

        public TestSceneRectanglePacking()
        {
            Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.2f),
                    new Dimension(GridSizeMode.Relative, 0.2f)
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.33f),
                    new Dimension(GridSizeMode.Relative, 0.33f),
                    new Dimension(GridSizeMode.Relative, 0.33f),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new DrawableBin(new ShelfRectanglePacker(new Vector2I(size))),
                        new DrawableBin(new ShelfWithRemainderRectanglePacker(new Vector2I(size))),
                        new DrawableBin(new MaximalRectanglePacker(new Vector2I(size), FitStrategy.First)),
                        new DrawableBin(new MaximalRectanglePacker(new Vector2I(size), FitStrategy.TopLeft)),
                        new DrawableBin(new MaximalRectanglePacker(new Vector2I(size), FitStrategy.BestLongSide)),
                    },
                    new Drawable[]
                    {
                        new DrawableBin(new MaximalRectanglePacker(new Vector2I(size), FitStrategy.BestShortSide)),
                        new DrawableBin(new MaximalRectanglePacker(new Vector2I(size), FitStrategy.SmallestArea)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.First, SplitStrategy.ShorterAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.First, SplitStrategy.ShorterLeftoverAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.BestShortSide, SplitStrategy.ShorterAxis))
                    },
                    new Drawable[]
                    {
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.BestShortSide, SplitStrategy.ShorterLeftoverAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.SmallestArea, SplitStrategy.ShorterAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.SmallestArea, SplitStrategy.ShorterLeftoverAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.TopLeft, SplitStrategy.ShorterAxis)),
                        new DrawableBin(new GuillotineRectanglePacker(new Vector2I(size), FitStrategy.TopLeft, SplitStrategy.ShorterLeftoverAxis))
                    }
                }
            });
        }

        private int minWidth = 1;
        private int maxWidth = size / 4;
        private int minHeight = 1;
        private int maxHeight = size / 4;
        private int singleWidth = size / 4;
        private int singleHeight = size / 4;

        [Test]
        public void TestSingle()
        {
            AddSliderStep("Width", 1, size, size / 4, w => singleWidth = w);
            AddSliderStep("Height", 1, size, size / 4, h => singleHeight = h);

            AddStep("Reset", resetAll);
            AddStep("Add", () => tryAddToAll(singleWidth, singleHeight));
        }

        [Test]
        public void TestMany()
        {
            AddSliderStep("Min width", 1, size, 1, w => minWidth = w);
            AddSliderStep("Max width", 2, size, size / 4, h => maxWidth = h);
            AddSliderStep("Min height", 1, size, 1, w => minHeight = w);
            AddSliderStep("Max height", 2, size, size / 4, h => maxHeight = h);

            AddStep("Reset", resetAll);
            AddUntilStep("Add until all filled", () => tryAddToAll(RNG.Next(minWidth, maxWidth), RNG.Next(minHeight, maxHeight)));
        }

        private void resetAll()
        {
            foreach (var b in bins)
                b.Reset();
        }

        private bool tryAddToAll(int width, int height)
        {
            bool canAddMore = false;

            foreach (var b in bins)
                canAddMore |= b.TryAdd(width, height);

            return !canAddMore;
        }

        private IEnumerable<DrawableBin> bins => this.ChildrenOfType<DrawableBin>();

        private partial class DrawableBin : Container
        {
            private readonly IRectanglePacker packer;
            private readonly BindableInt counter = new BindableInt();
            private readonly Container placed;
            private readonly SpriteText info;
            private bool blocked;

            public DrawableBin(IRectanglePacker packer)
            {
                this.packer = packer;

                Size = packer.BinSize;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Masking = true;
                BorderColour = Color4.White;
                BorderThickness = 3;

                AddRange(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    },
                    placed = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0.5f,
                                Colour = Color4.Black
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 5),
                                Children = new Drawable[]
                                {
                                    new TextFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Y,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        TextAnchor = Anchor.Centre,
                                        Text = packer.ToString() ?? string.Empty
                                    },
                                    info = new SpriteText
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre
                                    }
                                }
                            }
                        }
                    }
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                counter.BindValueChanged(c => info.Text = $"Count: {c.NewValue}");
            }

            public void Reset()
            {
                blocked = false;
                BorderColour = Color4.White;
                placed.Clear();
                packer.Reset();

                counter.Value = 0;
            }

            public bool TryAdd(int width, int height)
            {
                if (blocked)
                    return false;

                Vector2I? positionToPlace = packer.TryAdd(width, height);

                if (!positionToPlace.HasValue)
                {
                    blocked = true;
                    BorderColour = Color4.Red;
                    return false;
                }

                placed.Add(new Box
                {
                    Size = new Vector2(width, height),
                    Position = positionToPlace.Value,
                    Colour = getRandomColour()
                });

                counter.Value++;
                return true;
            }

            private static Color4 getRandomColour()
            {
                return new Color4(RNG.NextSingle(0.5f, 1f), RNG.NextSingle(0.5f, 1f), RNG.NextSingle(0.5f, 1f), 1f);
            }
        }
    }
}
