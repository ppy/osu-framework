﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseCheckboxes : TestCase
    {
        public override string Description => @"Checkboxes with clickable labels";

        public TestCaseCheckboxes()
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
                        new BasicCheckbox
                        {
                            LabelText = @"Basic Test"
                        },
                        new BasicCheckbox
                        {
                            LabelText = @"FadeDuration Test",
                            FadeDuration = 300
                        },
                        new ActionsTestCheckbox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };
        }
    }

    public class ActionsTestCheckbox : BasicCheckbox
    {
        public ActionsTestCheckbox()
        {
            Current.ValueChanged += v => this.RotateTo(v ? 45 : 0, 100);
        }
    }
}
