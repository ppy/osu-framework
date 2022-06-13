// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneCheckboxes : FrameworkTestScene
    {
        private readonly BasicCheckbox basic;

        public TestSceneCheckboxes()
        {
            BasicCheckbox swap, rotate;

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
                        basic = new BasicCheckbox
                        {
                            LabelText = @"Basic Test"
                        },
                        new BasicCheckbox
                        {
                            LabelText = @"FadeDuration Test",
                            FadeDuration = 300
                        },
                        swap = new BasicCheckbox
                        {
                            LabelText = @"Checkbox Position",
                        },
                        rotate = new BasicCheckbox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };

            swap.Current.ValueChanged += check => swap.RightHandedCheckbox = check.NewValue;
            rotate.Current.ValueChanged += e => rotate.RotateTo(e.NewValue ? 45 : 0, 100);
        }

        /// <summary>
        /// Test safety of <see cref="IHasCurrentValue{T}"/> implementation.
        /// This is shared across all UI elements.
        /// </summary>
        [Test]
        public void TestDirectToggle()
        {
            var testBindable = new Bindable<bool> { BindTarget = basic.Current };

            AddAssert("is unchecked", () => !basic.Current.Value);
            AddAssert("bindable unchecked", () => !testBindable.Value);

            AddStep("switch bindable directly", () => basic.Current.Value = true);

            AddAssert("is checked", () => basic.Current.Value);
            AddAssert("bindable checked", () => testBindable.Value);

            AddStep("change bindable", () => basic.Current = new Bindable<bool>());

            AddAssert("is unchecked", () => !basic.Current.Value);
            AddAssert("bindable unchecked", () => !testBindable.Value);
        }
    }
}
