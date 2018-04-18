// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSliderbar : TestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(BasicSliderBar<>), typeof(SliderBar<>) };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BindableDouble sliderBarValue; //keep a reference to avoid GC of the bindable
        private readonly SpriteText sliderbarText;

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

            SliderBar<double> sliderBar = new BasicSliderBar<double>
            {
                Size = new Vector2(200, 10),
                Position = new Vector2(25, 25),
                Color = Color4.White,
                SelectionColor = Color4.Pink,
                KeyboardStep = 1
            };

            sliderBar.Current.BindTo(sliderBarValue);

            Add(sliderBar);
            Add(sliderbarText);

            Add(sliderBar = new BasicSliderBar<double>
            {
                Size = new Vector2(200, 10),
                RangePadding = 20,
                Position = new Vector2(25, 45),
                Color = Color4.White,
                SelectionColor = Color4.Pink,
                KeyboardStep = 1,
            });

            sliderBar.Current.BindTo(sliderBarValue);

            AddSliderStep("Value", -10.0, 10.0, -10.0, v => sliderBarValue.Value = v);

            AddStep("Click at x = 50", () => sliderBar.TriggerOnClick(new InputState
            {
                Mouse = new MouseState
                {
                    Position = sliderBar.ToScreenSpace(sliderBar.DrawSize / 4)
                },
                Keyboard = new KeyboardState { Keys = new OpenTK.Input.Key[0] }
            }));

            AddAssert("Value == -6,25", () => sliderBarValue == -6.25);

            AddStep("Press left arrow key", () =>
            {
                var before = sliderBar.IsHovered;
                sliderBar.IsHovered = true;
                sliderBar.TriggerOnKeyDown(null, new KeyDownEventArgs
                {
                    Key = OpenTK.Input.Key.Left,
                });
                sliderBar.IsHovered = before;
            });

            AddAssert("Value == -7,25", () => sliderBarValue == -7.25);

            AddStep("Click at x = 150 with shift", () =>
            {
                var drawSize = sliderBar.DrawSize;
                drawSize.X *= 0.75f;
                sliderBar.TriggerOnClick(new InputState
                {
                    Mouse = new MouseState
                    {
                        Position = sliderBar.ToScreenSpace(drawSize)
                    },
                    Keyboard = new KeyboardState { Keys = new [] { OpenTK.Input.Key.LShift } }
                });
            });

            AddAssert("Value == 6", () => sliderBarValue == 6);
        }

        private void sliderBarValueChanged(double newValue)
        {
            sliderbarText.Text = $"Selected value: {newValue:N}";
        }
    }
}
