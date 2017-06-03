// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTooltip : TestCase
    {
        public override string Description => "Tooltip that shows when hovering a drawable";

        private Container testContainer;

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
                        }
                },
            });

            if (cursorlessTooltip)
                testContainer.Add(new TooltipContainer());
        }

        public override void Reset()
        {
            base.Reset();

            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddToggleStep("Cursor-less tooltip", state => generateTest(state));
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
    }
}
