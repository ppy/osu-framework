using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class Sliderbar : Container
    {
        private readonly double keyboardStep;
        private readonly double minValue, maxValue;
        private readonly BindableDouble selectedValue;
        private readonly Color4 color, selectedRangeColor;
        private readonly Box selectedRangeBox;
        private readonly Box sliderbarBox;
        private double valuesRange => maxValue - minValue;
        private double selectedRange;

        public Sliderbar(double minValue, double maxValue, BindableDouble selectedValue, Color4 color, Color4 selectedRangeColor)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.selectedValue = selectedValue;
            this.color = color;
            this.selectedRangeColor = selectedRangeColor;
            keyboardStep = 0.01;
            selectedRange = this.selectedValue - this.minValue;
            selectedValue.ValueChanged += SelectedValue_ValueChanged;

            Children = new Drawable[]
            {
                sliderbarBox = new Box
                {
                    Colour = color,
                    RelativeSizeAxes = Axes.Both,
                },
                selectedRangeBox = new Box
                {
                    Colour = selectedRangeColor,
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft
                }
            };
        }

        #region Disposal

        ~Sliderbar()
        {
            selectedValue.ValueChanged -= SelectedValue_ValueChanged;
        }

        #endregion

        protected override void Load(BaseGame game)
        {
            base.Load(game);
            updateVisualization();
        }

        protected override bool OnClick(InputState state)
        {
            handleMouseInput(state);
            return true;
        }

        protected override bool OnDrag(InputState state)
        {
            handleMouseInput(state);
            return true;
        }

        protected override bool OnDragStart(InputState state)
        {
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            return true;
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key == Key.Right)
                selectedValue.Value += keyboardStep;
            else if (args.Key == Key.Left)
                selectedValue.Value -= keyboardStep;
            return true;
        }

        private void SelectedValue_ValueChanged(object sender, EventArgs e)
        {
            if (selectedValue.Value > maxValue)
            {
                selectedValue.Value = maxValue;
                return;
            }

            if (selectedValue.Value < minValue)
            {
                selectedValue.Value = minValue;
                return;
            }
            updateVisualization();
        }

        private void handleMouseInput(InputState state)
        {
            var xPosition = state.Mouse.Position.X;
            if (xPosition < 0)
                xPosition = 0;
            if (xPosition > sliderbarBox.DrawWidth)
                xPosition = sliderbarBox.DrawWidth;
            double percentage = xPosition / sliderbarBox.DrawWidth;
            selectedRange = valuesRange * percentage;
            selectedValue.Value = minValue + selectedRange;
        }

        private void updateVisualization()
        {
            var percentage = selectedRange / valuesRange;
            selectedRangeBox.ScaleTo(new Vector2((float)percentage, 1), 300, EasingTypes.OutQuint);
        }
    }
}
