// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneSafeAreaContainer : FrameworkTestScene
    {
        private readonly BindableSafeArea safeAreaPadding = new BindableSafeArea();

        private readonly Box safeAreaTopOverlay;
        private readonly Box safeAreaBottomOverlay;
        private readonly Box safeAreaLeftOverlay;
        private readonly Box safeAreaRightOverlay;

        public TestSceneSafeAreaContainer()
        {
            var safeAreaGrid = new SafeAreaContainer
            {
                Name = "Padding Container",
                SafeAreaOverrideEdges = Edges.None,
                RelativeSizeAxes = Axes.Both,
                Child = createGridContainer(10, 10),
            };

            var safeAreaBackground = new SafeAreaContainer
            {
                Name = "Overriding Container",
                RelativeSizeAxes = Axes.Both,
                SafeAreaOverrideEdges = Edges.None,
                Child = new Box
                {
                    Name = "Overriding Background",
                    Colour = Color4.Blue,
                    RelativeSizeAxes = Axes.Both
                },
            };

            Child = new FillFlowContainer
            {
                Padding = new MarginPadding(10),
                Spacing = new Vector2(10),
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new MarginPaddingControlsContainer(safeAreaBackground, safeAreaGrid, safeAreaPadding)
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                    new SafeAreaDefiningContainer(safeAreaPadding)
                    {
                        Name = "Safe Area Target",
                        Size = new Vector2(500, 400),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Name = "Safe Area Target Background",
                                Colour = Color4.Red,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new Container
                            {
                                Position = new Vector2(50, 50),
                                Size = new Vector2(400, 300),
                                Children = new Drawable[]
                                {
                                    safeAreaBackground,
                                    safeAreaGrid,
                                }
                            },
                            safeAreaLeftOverlay = new Box
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Width = 0,
                                RelativeSizeAxes = Axes.Y,
                                Colour = Color4.Green,
                                Alpha = 0.2f
                            },
                            safeAreaRightOverlay = new Box
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Width = 0,
                                RelativeSizeAxes = Axes.Y,
                                Colour = Color4.Green,
                                Alpha = 0.2f
                            },
                            safeAreaTopOverlay = new Box
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Height = 0,
                                RelativeSizeAxes = Axes.X,
                                Colour = Color4.Green,
                                Alpha = 0.2f
                            },
                            safeAreaBottomOverlay = new Box
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Height = 0,
                                RelativeSizeAxes = Axes.X,
                                Colour = Color4.Green,
                                Alpha = 0.2f
                            }
                        }
                    }
                }
            };

            safeAreaPadding.ValueChanged += e => safeAreaPaddingChanged(e.NewValue);
        }

        private GridContainer createGridContainer(int rows, int columns)
        {
            Drawable[][] boxes = Enumerable.Range(1, rows).Select(row => Enumerable.Range(1, columns).Select(column => new Box
            {
                Colour = new Color4(1f, 0.2f + (row * 0.8f) / rows, 0.2f + (column * 0.8f) / columns, 1f),
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.7f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            } as Drawable).ToArray()).ToArray();

            return new GridContainer
            {
                Name = "Safe Contents",
                RelativeSizeAxes = Axes.Both,
                Content = boxes,
            };
        }

        private void safeAreaPaddingChanged(MarginPadding padding)
        {
            safeAreaLeftOverlay.Width = padding.Left;
            safeAreaRightOverlay.Width = padding.Right;
            safeAreaTopOverlay.Height = padding.Top;
            safeAreaBottomOverlay.Height = padding.Bottom;
        }

        private class MarginPaddingControlsContainer : FillFlowContainer
        {
            private readonly Bindable<float> safeAreaPaddingTop;
            private readonly Bindable<float> safeAreaPaddingBottom;
            private readonly Bindable<float> safeAreaPaddingLeft;
            private readonly Bindable<float> safeAreaPaddingRight;

            private readonly BindableSafeArea bindableSafeArea;

            public MarginPaddingControlsContainer(SafeAreaContainer safeAreaBackground, SafeAreaContainer safeAreaGrid, BindableSafeArea bindableSafeArea)
            {
                this.bindableSafeArea = bindableSafeArea;

                safeAreaPaddingTop = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingBottom = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingLeft = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingRight = new BindableFloat { MinValue = 0, MaxValue = 200 };

                Direction = FillDirection.Vertical;
                Spacing = new Vector2(10);
                Children = new Drawable[]
                {
                    new MarginPaddingControl(safeAreaBackground, safeAreaGrid, "Top", safeAreaPaddingTop, Edges.Top),
                    new MarginPaddingControl(safeAreaBackground, safeAreaGrid, "Bottom", safeAreaPaddingBottom, Edges.Bottom),
                    new MarginPaddingControl(safeAreaBackground, safeAreaGrid, "Left", safeAreaPaddingLeft, Edges.Left),
                    new MarginPaddingControl(safeAreaBackground, safeAreaGrid, "Right", safeAreaPaddingRight, Edges.Right),
                };

                safeAreaPaddingTop.ValueChanged += updateMarginPadding;
                safeAreaPaddingBottom.ValueChanged += updateMarginPadding;
                safeAreaPaddingLeft.ValueChanged += updateMarginPadding;
                safeAreaPaddingRight.ValueChanged += updateMarginPadding;
            }

            private void updateMarginPadding(ValueChangedEvent<float> e)
            {
                bindableSafeArea.Value = new MarginPadding
                {
                    Top = safeAreaPaddingTop.Value,
                    Bottom = safeAreaPaddingBottom.Value,
                    Left = safeAreaPaddingLeft.Value,
                    Right = safeAreaPaddingRight.Value,
                };
            }

            private class MarginPaddingControl : FillFlowContainer
            {
                public MarginPaddingControl(SafeAreaContainer safeAreaBackground, SafeAreaContainer safeAreaGrid, string title, Bindable<float> bindable, Edges edge)
                {
                    SpriteText valueText;
                    BasicCheckbox overrideCheckbox;
                    BasicCheckbox safeCheckbox;

                    Direction = FillDirection.Horizontal;
                    Spacing = new Vector2(20, 0);
                    AutoSizeAxes = Axes.Both;

                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = title,
                            Width = 60,
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        valueText = new SpriteText
                        {
                            Width = 50,
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        new BasicSliderBar<float>
                        {
                            Current = bindable,
                            Size = new Vector2(100, 20),
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        new SpriteText
                        {
                            Text = "Background Override",
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        overrideCheckbox = new BasicCheckbox(),
                        new SpriteText
                        {
                            Text = "Grid Override",
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        safeCheckbox = new BasicCheckbox()
                    };

                    overrideCheckbox.Current.ValueChanged += e =>
                    {
                        if (e.NewValue)
                            safeAreaBackground.SafeAreaOverrideEdges |= edge;
                        else
                            safeAreaBackground.SafeAreaOverrideEdges &= ~edge;
                    };

                    safeCheckbox.Current.ValueChanged += e =>
                    {
                        if (e.NewValue)
                            safeAreaGrid.SafeAreaOverrideEdges |= edge;
                        else
                            safeAreaGrid.SafeAreaOverrideEdges &= ~edge;
                    };

                    bindable.ValueChanged += e => valueText.Text = $"{e.NewValue:F1}";
                    bindable.TriggerChange();
                }
            }
        }
    }
}
