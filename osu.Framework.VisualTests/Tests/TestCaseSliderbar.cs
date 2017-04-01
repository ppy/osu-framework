// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseSliderbar : TestCase
    {
        public override string Description => @"Sliderbar tests.";
        private SliderBar<double> sliderBar;
        private BindableDouble sliderBarValue;
        private SpriteText sliderbarText;

        public override void Reset()
        {
            base.Reset();

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
                Bindable = sliderBarValue,
                Color = Color4.White,
                SelectionColor = Color4.Pink,
                KeyboardStep = 1
            };

            Add(sliderBar);
            Add(sliderbarText);

            Add(new BasicSliderBar<double>
            {
                Size = new Vector2(200, 10),
                RangePadding = 20,
                Position = new Vector2(25, 45),
                Color = Color4.White,
                SelectionColor = Color4.Pink,
                KeyboardStep = 1,
                Bindable = sliderBarValue,
            });
        }

        private void sliderBarValueChanged(object sender, EventArgs e)
        {
            sliderbarText.Text = $"Selected value: {sliderBarValue.Value:N}";
        }
    }
}
