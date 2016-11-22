using System;
using osu.Framework.Configuration;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseSliderbar : TestCase
    {
        public override string Name => @"Sliderbar";
        public override string Description => @"Sliderbar tests.";
        private SliderBar sliderBar;
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
            sliderBar = new SliderBar
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
        }

        private void sliderBarValueChanged(object sender, EventArgs e)
        {
            sliderbarText.Text = $"Selected value: {sliderBarValue.Value:N}";
        }
    }
}
