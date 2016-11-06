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
        public double KeyboardStep { get; set; }

        public double MinValue
        {
            get { return minValue; }
            set
            {
                minValue = value;
                selectedRange = SelectedValue - MinValue;
            }
        }

        public double MaxValue { get; set; }

        public BindableDouble SelectedValue
        {
            get { return selectedValue; }
            set
            {
                if (selectedValue != null)
                    selectedValue.ValueChanged -= SelectedValue_ValueChanged;
                selectedValue = value;
                selectedRange = SelectedValue - MinValue;
                selectedValue.ValueChanged += SelectedValue_ValueChanged;
            }
        }

        public Color4 Color
        {
            get { return sliderbarBox.Colour; }
            set { sliderbarBox.Colour = value; }
        }

        public Color4 SelectedRangeColor
        {
            get { return selectedRangeBox.Colour; }
            set { selectedRangeBox.Colour = value; }
        }

        private readonly Box selectedRangeBox;
        private readonly Box sliderbarBox;
        private double valuesRange => MaxValue - MinValue;
        private double selectedRange;
        private BindableDouble selectedValue;
        private double minValue;

        public Sliderbar()
        {
            Children = new Drawable[]
            {
                sliderbarBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft
                },
                selectedRangeBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft
                }
            };
        }

        #region Disposal

        ~Sliderbar()
        {
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (SelectedValue != null)
                SelectedValue.ValueChanged -= SelectedValue_ValueChanged;
            base.Dispose(isDisposing);
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
                SelectedValue.Value += KeyboardStep;
            else if (args.Key == Key.Left)
                SelectedValue.Value -= KeyboardStep;
            return true;
        }

        private void SelectedValue_ValueChanged(object sender, EventArgs e)
        {
            SelectedValue.Value = MathHelper.Clamp(SelectedValue.Value, MinValue, MaxValue);
            updateVisualization();
        }

        private void handleMouseInput(InputState state)
        {
            var xPosition = GetLocalPosition(state.Mouse.NativeState.Position).X;
            xPosition = MathHelper.Clamp(xPosition, 0, sliderbarBox.DrawWidth);
            double percentage = xPosition / sliderbarBox.DrawWidth;
            selectedRange = valuesRange * percentage;
            SelectedValue.Value = MinValue + selectedRange;
        }

        private void updateVisualization()
        {
            var percentage = selectedRange / valuesRange;
            selectedRangeBox.ScaleTo(new Vector2((float)percentage, 1), 300, EasingTypes.OutQuint);
        }
    }
}
