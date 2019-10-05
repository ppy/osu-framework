// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneCheckboxes : TestScene
    {
        public BasicCheckbox Basic { get; }
        public BasicCheckbox Swap { get; }
        public BasicCheckbox Rotate { get; }

        public TestSceneCheckboxes()
        {
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Padding = new MarginPadding(10),
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        Basic = new BasicCheckbox
                        {
                            LabelText = @"Basic Test"
                        },
                        new BasicCheckbox
                        {
                            LabelText = @"FadeDuration Test",
                            FadeDuration = 300
                        },
                        Swap = new BasicCheckbox
                        {
                            LabelText = @"Checkbox Position",
                        },
                        Rotate = new BasicCheckbox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };
        }
    }
}
