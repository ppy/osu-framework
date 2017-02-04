using System;
using osu.Framework.Configuration;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transformations;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseSliderbar : TestCase
    {
        public override string Name => @"Sliderbar";
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
            sliderBar = new TestSliderBar<double>
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

            Add(new TestSliderBar<double>
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
        
        private class TestSliderBar<T> : SliderBar<T> where T : struct
        {
            public Color4 Color
            {
                get { return Box.Colour; }
                set { Box.Colour = value; }
            }
    
            public Color4 SelectionColor
            {
                get { return SelectionBox.Colour; }
                set { SelectionBox.Colour = value; }
            }
    
            protected readonly Box SelectionBox;
            protected readonly Box Box;
    
            public TestSliderBar()
            {
                Children = new Drawable[]
                {
                    Box = new Box { RelativeSizeAxes = Axes.Both },
                    SelectionBox = new Box { RelativeSizeAxes = Axes.Both }
                };
            }

            protected override void UpdateValue(float value)
            {
                SelectionBox.ScaleTo(
                    new Vector2(value, 1),
                    300, EasingTypes.OutQuint);
            }
        }
    }
}
