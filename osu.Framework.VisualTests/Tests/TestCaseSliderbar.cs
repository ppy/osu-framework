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
        private Sliderbar sliderbar;
        private BindableDouble sliderBarSelectedValue;
        private SpriteText sliderbarText;

        public override void Reset()
        {
            base.Reset();

            sliderBarSelectedValue = new BindableDouble(8);
            sliderBarSelectedValue.ValueChanged += sliderBarSelectedValueChanged;

            sliderbarText = new SpriteText
            {
                Text = $"Selected value: {sliderBarSelectedValue.Value}",
                Position = new Vector2(25, 0)
            };
            sliderbar = new Sliderbar
            {
                Size = new Vector2(200, 10),
                Position = new Vector2(25, 25),
                SelectedValue = sliderBarSelectedValue,
                MinValue = -10,
                MaxValue = 10,
                KeyboardStep = 0.01,
                Color = Color4.White,
                SelectedRangeColor = Color4.Pink
            };

            Add(sliderbar);
            Add(sliderbarText);
        }

        private void sliderBarSelectedValueChanged(object sender, EventArgs e)
        {
            sliderbarText.Text = $"Selected value: {sliderBarSelectedValue.Value:N}";
        }
    }
}
