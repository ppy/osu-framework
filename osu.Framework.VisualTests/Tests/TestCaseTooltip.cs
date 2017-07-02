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

        private Container testContainer;

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
            testContainer.Add(new FillFlowContainer
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
                    new Container()
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new TooltipSpriteText("Nested tooltip; uses no cursor in all cases!"),
                            new TooltipContainer(),
                        }
                    },
                },
            });

            testContainer.Add(makeBox(Anchor.BottomLeft));
            testContainer.Add(makeBox(Anchor.TopRight));
            testContainer.Add(makeBox(Anchor.BottomRight));

            CursorContainer cursor = null;
            if (!cursorlessTooltip)
            {
                cursor = new RectangleCursorContainer();
                testContainer.Add(cursor);
            }

            testContainer.Add(new TooltipContainer(cursor));
        }

        public override void Reset()
        {
            base.Reset();

            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddToggleStep("Cursor-less tooltip", generateTest);
            generateTest(false);
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
