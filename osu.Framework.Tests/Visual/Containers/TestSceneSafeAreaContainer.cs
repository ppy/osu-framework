// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneSafeAreaContainer : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(SafeAreaContainer),
            typeof(SafeAreaTargetContainer),
            typeof(EdgeSnappingContainer),
            typeof(SnapTargetContainer)
        };

        private readonly Bindable<MarginPadding> safeAreaPadding = new BindableMarginPadding();

        private readonly Box safeAreaTopOverlay;
        private readonly Box safeAreaBottomOverlay;
        private readonly Box safeAreaLeftOverlay;
        private readonly Box safeAreaRightOverlay;

        public TestSceneSafeAreaContainer()
        {
            var safeContainer = new SafeAreaContainer
            {
                Name = "Padding Container",
                SafeEdges = Edges.None,
                SnappedEdges = Edges.None,
                RelativeSizeAxes = Axes.Both,
                Child = createGridContainer(),
            };

            var snappingContainer = new SafeAreaContainer
            {
                Name = "Snapping Container",
                RelativeSizeAxes = Axes.Both,
                SafeEdges = Edges.None,
                SnappedEdges = Edges.None,
                Child = new Box
                {
                    Name = "Snapping Background",
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
                    new MarginPaddingControlsContainer(snappingContainer, safeContainer, safeAreaPadding)
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                    new SafeAreaTargetContainer
                    {
                        Name = "Safe Area Target",
                        Size = new Vector2(500, 400),
                        SafeAreaPadding = safeAreaPadding,
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
                                    snappingContainer,
                                    safeContainer,
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
        }

        private GridContainer createGridContainer()
        {
            var rows = new List<Drawable[]>();

            for (int i = 0; i < 10; i++)
            {
                var row = new List<Drawable>();

                for (int j = 0; j < 10; j++)
                {
                    row.Add(new Box
                    {
                        Colour = Color4.White,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.7f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    });
                }

                rows.Add(row.ToArray());
            }

            return new GridContainer
            {
                Name = "Safe Contents",
                RelativeSizeAxes = Axes.Both,
                Content = rows.ToArray()
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            safeAreaPadding.ValueChanged += e => safeAreaPaddingChanged(e.NewValue);
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

            private readonly Bindable<MarginPadding> bindableMarginPadding;

            public MarginPaddingControlsContainer(SafeAreaContainer snappingContainer, SafeAreaContainer safeContainer, Bindable<MarginPadding> bindableMarginPadding)
            {
                this.bindableMarginPadding = bindableMarginPadding;

                safeAreaPaddingTop = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingBottom = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingLeft = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingRight = new BindableFloat { MinValue = 0, MaxValue = 200 };

                Direction = FillDirection.Vertical;
                Spacing = new Vector2(10);
                Children = new Drawable[]
                {
                    new MarginPaddingControl(snappingContainer, safeContainer, "Top", safeAreaPaddingTop, Edges.Top),
                    new MarginPaddingControl(snappingContainer, safeContainer, "Bottom", safeAreaPaddingBottom, Edges.Bottom),
                    new MarginPaddingControl(snappingContainer, safeContainer, "Left", safeAreaPaddingLeft, Edges.Left),
                    new MarginPaddingControl(snappingContainer, safeContainer, "Right", safeAreaPaddingRight, Edges.Right),
                };

                safeAreaPaddingTop.ValueChanged += updateMarginPadding;
                safeAreaPaddingBottom.ValueChanged += updateMarginPadding;
                safeAreaPaddingLeft.ValueChanged += updateMarginPadding;
                safeAreaPaddingRight.ValueChanged += updateMarginPadding;
            }

            private void updateMarginPadding(ValueChangedEvent<float> e)
            {
                bindableMarginPadding.Value = new MarginPadding
                {
                    Top = safeAreaPaddingTop.Value,
                    Bottom = safeAreaPaddingBottom.Value,
                    Left = safeAreaPaddingLeft.Value,
                    Right = safeAreaPaddingRight.Value,
                };
            }

            private class MarginPaddingControl : FillFlowContainer
            {
                public MarginPaddingControl(SafeAreaContainer snappingContainer, SafeAreaContainer safeContainer, string title, Bindable<float> bindable, Edges edge)
                {
                    SpriteText valueText;
                    BasicCheckbox snapCheckbox;
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
                            Text = "Snap",
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        snapCheckbox = new BasicCheckbox(),
                        new SpriteText
                        {
                            Text = "Safe",
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        safeCheckbox = new BasicCheckbox()
                    };

                    snapCheckbox.Current.ValueChanged += e =>
                    {
                        if (e.NewValue)
                            snappingContainer.SnappedEdges |= edge;
                        else
                            snappingContainer.SnappedEdges &= ~edge;
                    };

                    safeCheckbox.Current.ValueChanged += e =>
                    {
                        if (e.NewValue)
                            safeContainer.SafeEdges |= edge;
                        else
                            safeContainer.SafeEdges &= ~edge;
                    };

                    bindable.ValueChanged += e => valueText.Text = $"{e.NewValue:F1}";
                    bindable.TriggerChange();
                }
            }
        }
    }
}
