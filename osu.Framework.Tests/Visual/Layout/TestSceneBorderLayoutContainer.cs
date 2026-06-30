// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    [TestFixture]
    public partial class TestSceneBorderLayoutContainer : FrameworkTestScene
    {
        private readonly BorderLayoutContainer borderLayout;

        public TestSceneBorderLayoutContainer()
        {
            Box top, bottom, left, right;

            Children = new Drawable[]
            {
                new BorderLayoutContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Center = borderLayout = new BorderLayoutContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Top = top = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 100,
                            Colour = Color4.Red,
                        },
                        Bottom = bottom = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 100,
                            Colour = Color4.Blue,
                        },
                        Left = left = new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 100,
                            Colour = Color4.Yellow,
                        },
                        Right = right = new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 100,
                            Colour = Color4.Green,
                        },
                        Center = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Gray
                        }
                    },
                    Left = new LayoutEdgeParameters("Left")
                    {
                        ResizeAction = value => left.Width = value,
                        VisibilityAction = value => left.Alpha = value ? 1 : 0,
                        SpacingAction = value => borderLayout.Spacing = borderLayout.Spacing with { Left = value },
                    },
                    Right = new LayoutEdgeParameters("Right")
                    {
                        ResizeAction = value => right.Width = value,
                        VisibilityAction = value => right.Alpha = value ? 1 : 0,
                        SpacingAction = value => borderLayout.Spacing = borderLayout.Spacing with { Right = value }
                    },
                    Top = new LayoutEdgeParameters("Top")
                    {
                        ResizeAction = value => top.Height = value,
                        VisibilityAction = value => top.Alpha = value ? 1 : 0,
                        SpacingAction = value => borderLayout.Spacing = borderLayout.Spacing with { Top = value }
                    },
                    Bottom = new LayoutEdgeParameters("Bottom")
                    {
                        ResizeAction = value => bottom.Height = value,
                        VisibilityAction = value => bottom.Alpha = value ? 1 : 0,
                        SpacingAction = value => borderLayout.Spacing = borderLayout.Spacing with { Bottom = value }
                    },
                },
            };
        }

        [Test]
        public void TestBorderLayout()
        {
            AddStep("horizontal layout direction", () => borderLayout.LayoutDirection = Direction.Horizontal);
            AddStep("vertical layout direction", () => borderLayout.LayoutDirection = Direction.Vertical);
            AddSliderStep("total spacing", 0f, 100f, 0f, value => borderLayout.Spacing = new MarginPadding(value));
        }

        private partial class LayoutEdgeParameters : Container
        {
            public required Action<float> ResizeAction { private get; init; }
            public required Action<float> SpacingAction { private get; init; }

            public Action<bool>? VisibilityAction { private get; init; }

            private readonly BindableFloat size = new BindableFloat(100)
            {
                MinValue = 0,
                MaxValue = 300,
            };

            private readonly BindableFloat spacing = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 100,
            };

            private readonly BindableBool visible = new BindableBool(true);

            private readonly Drawable visibilityGroup;

            protected override Container<Drawable> Content { get; }

            public LayoutEdgeParameters(string title)
            {
                AutoSizeAxes = Axes.Y;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Padding = new MarginPadding(5);

                Width = 150;

                InternalChild = Content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Text = $"{title}:",
                            Font = new FontUsage(size: 18)
                        },
                        visibilityGroup = new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            ColumnDimensions = new[] { new Dimension(), new Dimension(GridSizeMode.AutoSize) },
                            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Visible",
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = new FontUsage(size: 16),
                                    },
                                    new BasicCheckbox
                                    {
                                        Current = visible,
                                        Scale = new Vector2(0.9f)
                                    }
                                },
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new BasicSliderBar<float>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 24,
                                    BackgroundColour = Color4.RoyalBlue.Darken(0.75f),
                                    SelectionColour = Color4.RoyalBlue,
                                    FocusColour = Color4.RoyalBlue,
                                    Current = size,
                                },
                                new SpriteText
                                {
                                    Text = "Size",
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Font = new FontUsage(size: 16)
                                },
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new BasicSliderBar<float>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 24,
                                    BackgroundColour = Color4.RoyalBlue.Darken(0.75f),
                                    SelectionColour = Color4.RoyalBlue,
                                    FocusColour = Color4.RoyalBlue,
                                    Current = spacing,
                                },
                                new SpriteText
                                {
                                    Text = "Spacing",
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Font = new FontUsage(size: 16)
                                },
                            }
                        },
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                size.BindValueChanged(e => ResizeAction.Invoke(e.NewValue), true);
                spacing.BindValueChanged(e => SpacingAction.Invoke(e.NewValue), true);

                if (VisibilityAction != null)
                    visible.BindValueChanged(e => VisibilityAction?.Invoke(e.NewValue), true);
                else
                    visibilityGroup.Hide();
            }
        }
    }
}
