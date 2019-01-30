// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSliderbar : ManualInputManagerTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(BasicSliderBar<>), typeof(SliderBar<>) };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableDouble sliderBarValue; //keep a reference to avoid GC of the bindable
        private readonly SpriteText sliderbarText;
        private readonly SliderBar<double> sliderBar;

        public TestCaseSliderbar()
        {
            sliderBarValue = new BindableDouble(8)
            {
                MinValue = -10,
                MaxValue = 10
            };
            sliderBarValue.ValueChanged += sliderBarValueChanged;

            sliderbarText = new SpriteText
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
            Add(sliderbarText);

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
        public void Basic() {
            AddSliderStep("Value", -10.0, 10.0, -10.0, v => sliderBarValue.Value = v);

            AddStep("Click at x = 50", () =>
            {
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(sliderBar.DrawSize / 4));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("Value == -6,25", () => sliderBarValue == -6.25);

            AddStep("Press left arrow key", () =>
            {
                var before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                InputManager.PressKey(Key.Left);
                InputManager.ReleaseKey(Key.Left);
                sliderBar.IsHovered = before;
            });

            AddAssert("Value == -7,25", () => sliderBarValue == -7.25);

            AddStep("Click at x = 150 with shift", () =>
            {
                var drawSize = sliderBar.DrawSize;
                drawSize.X *= 0.75f;
                InputManager.PressKey(Key.LShift);
                InputManager.MoveMouseTo(sliderBar.ToScreenSpace(drawSize));
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });

            AddAssert("Value == 6", () => sliderBarValue == 6);
        }

        private void sliderBarValueChanged(double newValue)
        {
            sliderbarText.Text = $"Selected value: {newValue:N}";
        }
    }
}
