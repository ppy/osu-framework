// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    internal abstract class EmptyToolWindow : OverlayContainer
    {
        public const float WIDTH = 500;
        public const float HEIGHT = 600;

        private const float button_width = 140;
        private const float button_height = 40;

        private readonly Container toolbars;
        public FillFlowContainer ToolbarContent { get; private set; }

        protected readonly Container BodyContent;

        protected EmptyToolWindow(string title, string keyHelpText)
        {
            AutoSizeAxes = Axes.X;
            Height = HEIGHT;

            Masking = true; // for cursor masking

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
                                    toolbars = new Container
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
                                        },
                                    }
                                }
                            },
                        },
                        new Drawable[]
                        {
                            BodyContent = new TooltipContainer
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                            },
                        }
                    },
                },
                new CursorContainer()
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToolbar();
        }

        public void AddButton(string text, Action action)
        {
            ToolbarContent.Add(new BasicButton
            {
                Size = new Vector2(button_width, button_height),
                Text = text,
                Action = action
            });
        }

        public FillFlowContainer AddToolbar()
        {
            toolbars.Add(ToolbarContent = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(5),
                Padding = new MarginPadding(5),
            });

            SetCurrentToolbar(ToolbarContent);
            return ToolbarContent;
        }

        public void SetCurrentToolbar(FillFlowContainer newToolbar)
        {
            Debug.Assert(newToolbar.Parent == toolbars, "The passed toolbar is not in this set of toolbars");

            ToolbarContent = newToolbar;
            ToolbarContent.Show();

            foreach (var toolbar in toolbars)
            {
                if (toolbar.ChildID == 1)
                    continue; // Skip background box

                if (toolbar == ToolbarContent)
                    continue;

                toolbar.Hide();
            }
        }

        protected override void PopIn() => this.FadeIn(100);

        protected override void PopOut() => this.FadeOut(100);
    }

    internal abstract class ToolWindow : EmptyToolWindow
    {
        protected readonly ScrollContainer<Drawable> ScrollContent;

        protected readonly FillFlowContainer MainHorizontalContent;

        protected ToolWindow(string title, string keyHelpText)
            : base(title, keyHelpText)
        {
            BodyContent.Add(MainHorizontalContent = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    ScrollContent = new BasicScrollContainer<Drawable>
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = WIDTH
                    }
                }
            });
        }
    }
}
