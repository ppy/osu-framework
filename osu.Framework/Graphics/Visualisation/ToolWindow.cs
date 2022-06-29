// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    internal abstract class ToolWindow : OverlayContainer
    {
        public const float WIDTH = 500;
        public const float HEIGHT = 600;

        private const float button_width = 140;
        private const float button_height = 40;

        protected readonly FillFlowContainer ToolbarContent;

        protected readonly FillFlowContainer MainHorizontalContent;

        protected ScrollContainer<Drawable> ScrollContent;

        protected readonly SearchContainer SearchContainer;

        protected ToolWindow(string title, string keyHelpText, bool supportsSearch = false)
        {
            AutoSizeAxes = Axes.X;
            Height = HEIGHT;

            Masking = true; // for cursor masking

            BasicTextBox queryTextBox;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.GreenDark,
                    RelativeSizeAxes = Axes.Both,
                    Depth = 0
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Distributed),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new TitleBar(title, keyHelpText, this),
                                    new Container //toolbar
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                Colour = FrameworkColour.BlueGreenDark,
                                                RelativeSizeAxes = Axes.Both,
                                            },
                                            ToolbarContent = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Spacing = new Vector2(5),
                                                Padding = new MarginPadding(5),
                                            },
                                        },
                                    }
                                }
                            },
                        },
                        new Drawable[]
                        {
                            new TooltipContainer
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Child = MainHorizontalContent = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Direction = FillDirection.Horizontal,
                                    Child = new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = WIDTH,
                                        RowDimensions = new[]
                                        {
                                            new Dimension(GridSizeMode.AutoSize),
                                            new Dimension(GridSizeMode.Distributed)
                                        },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                queryTextBox = new BasicTextBox
                                                {
                                                    Width = WIDTH,
                                                    Height = 30,
                                                    PlaceholderText = "Search...",
                                                    Alpha = supportsSearch ? 1 : 0,
                                                }
                                            },
                                            new Drawable[]
                                            {
                                                ScrollContent = new BasicScrollContainer<Drawable>
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Child = SearchContainer = new SearchContainer
                                                    {
                                                        AutoSizeAxes = Axes.Y,
                                                        RelativeSizeAxes = Axes.X,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                },
                new CursorContainer()
            });

            queryTextBox.Current.BindValueChanged(term => SearchContainer.SearchTerm = term.NewValue, true);
        }

        protected void AddButton(string text, Action action)
        {
            ToolbarContent.Add(new BasicButton
            {
                Size = new Vector2(button_width, button_height),
                Text = text,
                Action = action
            });
        }

        protected override void PopIn() => this.FadeIn(100);

        protected override void PopOut() => this.FadeOut(100);
    }
}
