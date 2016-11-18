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
    public class SliderBar : Container
    {
        public double KeyboardStep { get; set; } = 0.01;

        public double MinValue
        {
            get { return Bindable.MinValue; }
            set
            {
                Bindable.MinValue = value;
                sliderBarPartsSelected = Bindable - MinValue;
            }
        }

        public double MaxValue
        {
            get { return Bindable.MaxValue; }
            set { Bindable.MaxValue = value; }
        }

        public BindableDouble Bindable
        {
            get { return bindable; }
            set
            {
                if (bindable != null)
                    bindable.ValueChanged -= bindableValueChanged;
                bindable = value;
                sliderBarPartsSelected = Bindable - MinValue;
                bindable.ValueChanged += bindableValueChanged;
            }
        }

        public Color4 Color
        {
            get { return sliderBarBox.Colour; }
            set { sliderBarBox.Colour = value; }
        }

        public Color4 SelectedRangeColor
        {
            get { return sliderBarSelectionBox.Colour; }
            set { sliderBarSelectionBox.Colour = value; }
        }

        private readonly Box sliderBarSelectionBox;
        private readonly Box sliderBarBox;
        private double sliderBarParts => MaxValue - MinValue;
        private double sliderBarPartsSelected;
        private BindableDouble bindable;

        public SliderBar()
        {
            Children = new Drawable[]
            {
                sliderBarBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft
                },
                sliderBarSelectionBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft
                }
            };
        }

        #region Disposal

        ~SliderBar()
        {
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (Bindable != null)
                Bindable.ValueChanged -= bindableValueChanged;
            base.Dispose(isDisposing);
        }

        #endregion

        protected override void LoadComplete()
        {
            base.LoadComplete();
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

        protected override bool OnDragStart(InputState state) => true;

        protected override bool OnDragEnd(InputState state) => true;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            double clampedBindable;
            switch (args.Key)
            {
                case Key.Right:
                    clampedBindable = MathHelper.Clamp(Bindable.Value + KeyboardStep, MinValue, MaxValue);
                    sliderBarPartsSelected = clampedBindable - MinValue;
                    Bindable.Value = clampedBindable;
                    return true;
                case Key.Left:
                    clampedBindable = MathHelper.Clamp(Bindable.Value - KeyboardStep, MinValue, MaxValue);
                    sliderBarPartsSelected = clampedBindable - MinValue;
                    Bindable.Value = clampedBindable;
                    return true;
                default:
                    return false;
            }
        }

        private void bindableValueChanged(object sender, EventArgs e) => updateVisualization();

        private void handleMouseInput(InputState state)
        {
            var xPosition = GetLocalPosition(state.Mouse.NativeState.Position).X;
            xPosition = MathHelper.Clamp(xPosition, 0, sliderBarBox.DrawWidth);
            double percentage = xPosition / sliderBarBox.DrawWidth;
            sliderBarPartsSelected = sliderBarParts * percentage;
            Bindable.Value = MinValue + sliderBarPartsSelected;
        }

        private void updateVisualization() => sliderBarSelectionBox.ScaleTo(new Vector2((float)(sliderBarPartsSelected / sliderBarParts), 1), 300, EasingTypes.OutQuint);
    }
}
