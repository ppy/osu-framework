// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTooltip : TestCase
    {
        public override string Description => "Tooltip that shows when hovering a drawable";

        private readonly Container testContainer;

        public TestCaseTooltip()
        {
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddToggleStep("Cursor-less tooltip", generateTest);
            generateTest(false);
        }

        private TooltipBox makeBox(Anchor anchor)
        {
            return new TooltipBox
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.2f),
                Anchor = anchor,
                Origin = anchor,
                Colour = Color4.Blue,
                TooltipText = $"{anchor}",
            };
        }

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
                            new TooltipSpriteText("this one too!"),
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

        private class TooltipSpriteText : Container, IHasTooltip
        {
            private readonly SpriteText text;

            public string TooltipText => text.Text;

            public TooltipSpriteText(string tooltipText)
            {
                AutoSizeAxes = Axes.Both;
                Children = new[]
                {
                    text = new SpriteText
                    {
                        Text = tooltipText,
                    }
                };
            }
        }

        private class TooltipTooltipContainer : TooltipContainer, IHasTooltip
        {
            public string TooltipText { get; set; }

            public TooltipTooltipContainer(string tooltipText)
            {
                TooltipText = tooltipText;
            }
        }

        private class TooltipTextbox : TextBox, IHasTooltip
        {
            public string TooltipText => Text;
        }

        private class TooltipBox : Box, IHasTooltip
        {
            public string TooltipText { get; set; }

            public override bool HandleInput => true;
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
