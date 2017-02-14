// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseCheckBox : TestCase
    {
        public override string Name => @"Checkboxes";

        public override string Description => @"CheckBoxes with clickable labels";

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new FlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Spacing = new Vector2(0, 10),
                    Padding = new MarginPadding(10),
                    AutoSizeAxes = Axes.Both,
                    Direction = FlowDirections.Vertical,
                    Children = new Drawable[]
                    {
                        new BasicCheckBox
                        {
                            LabelText = @"Basic Test"
                        },
                        new BasicCheckBox
                        {
                            LabelText = @"FadeDuration Test",
                            FadeDuration = 300
                        },
                        new ActionsTestCheckBox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };
        }
    }

    public class ActionsTestCheckBox : BasicCheckBox
    {
        protected override void OnChecked()
        {
            base.OnChecked();
            RotateTo(45, 100);
        }

        protected override void OnUnchecked()
        {
            base.OnUnchecked();
            RotateTo(0, 100);
        }
    }
}
