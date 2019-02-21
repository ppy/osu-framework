// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.TestCaseUserInterface
{
    public class TestCaseSliderBar : ManualInputManagerTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(BasicSliderBar<>), typeof(SliderBar<>) };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableDouble sliderBarValue; //keep a reference to avoid GC of the bindable
        private readonly SpriteText sliderBarText;
        private readonly SliderBar<double> sliderBar;

        public TestCaseSliderBar()
        {
            sliderBarValue = new BindableDouble(8)
            {
                MinValue = -10,
                MaxValue = 10
            };
            sliderBarValue.ValueChanged += sliderBarValueChanged;

            sliderBarText = new SpriteText
            {
                Text = $"Selected value: {sliderBarValue.Value}",
                Position = new Vector2(25, 0)
            };

            sliderBar = new BasicSliderBar<double>
            {
                Size = new Vector2(200, 10),
                Position = new Vector2(25, 25),
                BackgroundColour = Color4.White,
                SelectionColour = Color4.Pink,
                KeyboardStep = 1,
                Current = sliderBarValue
            };

            Add(sliderBar);
            Add(sliderBarText);

            Add(sliderBar = new BasicSliderBar<double>
            {
                Size = new Vector2(200, 10),
                RangePadding = 20,
                Position = new Vector2(25, 45),
                BackgroundColour = Color4.White,
                SelectionColour = Color4.Pink,
                KeyboardStep = 1,
                Current = sliderBarValue
            });

            Add(new BasicSliderBar<double>
            {
                TransferValueOnCommit = true,
                Size = new Vector2(200, 10),
                RangePadding = 20,
                Position = new Vector2(25, 65),
                BackgroundColour = Color4.White,
                SelectionColour = Color4.Pink,
                KeyboardStep = 1,
                Current = sliderBarValue
            });
        }

        [Test]
        public void Basic()
        {
            AddSliderStep("Value", sliderBarValue.MinValue, sliderBarValue.MaxValue, 0, v => sliderBarValue.Value = v);

            AddStep("Click at 25% mark", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(new Vector2(sliderBar.DrawWidth * 0.25f, sliderBar.DrawHeight * 0.5f)));
                InputManager.Click(MouseButton.Left);
            });

            // We're translating to/from screen-space coordinates for click coordinates so we want to be more lenient with the value comparisons in this test
            AddAssert("Value == -6.25", () => Precision.AlmostEquals(sliderBarValue, -6.25, Precision.FLOAT_EPSILON));

            AddStep("Press left arrow key", () =>
            {
                var before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                sliderBar.IsHovered = before;
            });

            AddAssert("Value == -7.25", () => Precision.AlmostEquals(sliderBarValue, -7.25, Precision.FLOAT_EPSILON));

            AddStep("Click at 75% mark, holding shift", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(new Vector2(sliderBar.DrawWidth * 0.75f, sliderBar.DrawHeight * 0.5f)));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });

            AddAssert("Value == 6", () => Precision.AlmostEquals(sliderBarValue, 6, Precision.FLOAT_EPSILON));
        }

        private void sliderBarValueChanged(double newValue)
        {
            sliderBarText.Text = $"Selected value: {newValue:N}";
        }
    }
}
