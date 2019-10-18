// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTooltip : FrameworkTestScene
    {
        private readonly Container testContainer;

        public TestSceneTooltip()
        {
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddToggleStep("Cursor-less tooltip", generateTest);
            generateTest(false);
        }

        private TooltipBox makeBox(Anchor anchor) =>
            new TooltipBox
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.2f),
                Anchor = anchor,
                Origin = anchor,
                Colour = Color4.Blue,
                TooltipText = $"{anchor}",
            };

        private void generateTest(bool cursorlessTooltip)
        {
            testContainer.Clear();

            CursorContainer cursor = null;

            if (!cursorlessTooltip)
            {
                cursor = new RectangleCursorContainer();
                testContainer.Add(cursor);
            }

            TooltipContainer ttc;
            testContainer.Add(ttc = new TooltipContainer(cursor)
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new TooltipBox
                            {
                                TooltipText = "Outer Tooltip",
                                Colour = Color4.CornflowerBlue,
                                Size = new Vector2(300, 300),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            },
                            new TooltipBox
                            {
                                TooltipText = "Inner Tooltip",
                                Size = new Vector2(150, 150),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new TooltipSpriteText("this text has a tooltip!"),
                            new InstantTooltipSpriteText("this text has an instant tooltip!"),
                            new CustomTooltipSpriteText("this one is custom!"),
                            new CustomTooltipSpriteText("this one is also!"),
                            new TooltipSpriteText("this text has an empty tooltip!", string.Empty),
                            new TooltipSpriteText("this text has a nulled tooltip!", null),
                            new TooltipTextbox
                            {
                                Text = "with real time updates!",
                                Size = new Vector2(400, 30),
                            },
                            new TooltipContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Child = new TooltipSpriteText("Nested tooltip; uses no cursor in all cases!"),
                            },
                            new TooltipTooltipContainer("This tooltip container has a tooltip itself!")
                            {
                                AutoSizeAxes = Axes.Both,
                                Child = new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Child = new TooltipSpriteText("Nested tooltip; uses no cursor in all cases; parent TooltipContainer has a tooltip"),
                                }
                            },
                            new Container
                            {
                                Child = new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 8),
                                    Children = new[]
                                    {
                                        new Container
                                        {
                                            Child = new Container
                                            {
                                                Child = new TooltipSpriteText("Tooltip within containers with zero size; i.e. parent is never hovered."),
                                            }
                                        },
                                        new Container
                                        {
                                            Child = new TooltipSpriteText("Other tooltip within containers with zero size; different nesting; overlap."),
                                        }
                                    }
                                }
                            }
                        },
                    }
                }
            });

            ttc.Add(makeBox(Anchor.BottomLeft));
            ttc.Add(makeBox(Anchor.TopRight));
            ttc.Add(makeBox(Anchor.BottomRight));
        }

        private class CustomTooltipSpriteText : Container, IHasCustomTooltip
        {
            public object TooltipContent { get; }

            public CustomTooltipSpriteText(string displayedContent, string tooltipContent = null)
            {
                TooltipContent = new CustomContent(tooltipContent ?? displayedContent);

                AutoSizeAxes = Axes.Both;
                Children = new[]
                {
                    new SpriteText
                    {
                        Text = displayedContent,
                    }
                };
            }

            public ITooltip GetCustomTooltip() => new CustomTooltip();

            private class CustomContent
            {
                public readonly string Text;

                public CustomContent(string text)
                {
                    Text = text;
                }
            }

            private class CustomTooltip : TooltipContainer.Tooltip
            {
                private static int i;

                public CustomTooltip()
                {
                    AddRangeInternal(new Drawable[]
                    {
                        new Box
                        {
                            Anchor = Anchor.BottomLeft,
                            Colour = Color4.Black,
                            RelativeSizeAxes = Axes.X,
                            Height = 12,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Font = FontUsage.Default.With(size: 12),
                            Colour = Color4.Yellow,
                            Text = $"Custom tooltip instance {i++}"
                        }
                    });
                }

                public override bool SetContent(object content)
                {
                    if (!(content is CustomContent custom))
                        return false;

                    base.SetContent(custom.Text);
                    return true;
                }
            }
        }

        private class TooltipSpriteText : Container, IHasTooltip
        {
            public string TooltipText { get; }

            public TooltipSpriteText(string displayedContent)
                : this(displayedContent, displayedContent)
            {
            }

            public TooltipSpriteText(string displayedContent, string tooltipContent)
            {
                this.TooltipText = tooltipContent;

                AutoSizeAxes = Axes.Both;
                Children = new[]
                {
                    new SpriteText
                    {
                        Text = displayedContent,
                    }
                };
            }
        }

        private class InstantTooltipSpriteText : TooltipSpriteText, IHasAppearDelay
        {
            public InstantTooltipSpriteText(string tooltipContent)
                : base(tooltipContent, tooltipContent)
            {
            }

            public double AppearDelay => 0;
        }

        private class TooltipTooltipContainer : TooltipContainer, IHasTooltip
        {
            public string TooltipText { get; set; }

            public TooltipTooltipContainer(string tooltipText)
            {
                TooltipText = tooltipText;
            }
        }

        private class TooltipTextbox : BasicTextBox, IHasTooltip
        {
            public string TooltipText => Text;
        }

        private class TooltipBox : Box, IHasTooltip
        {
            public string TooltipText { get; set; }
        }

        private class RectangleCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new RectangleCursor();

            private class RectangleCursor : Box
            {
                public RectangleCursor()
                {
                    Size = new Vector2(20, 40);
                }
            }
        }
    }
}
