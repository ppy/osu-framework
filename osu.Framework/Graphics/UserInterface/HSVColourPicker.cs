// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Control that allows for specifying a colour using the hue-saturation-value (HSV) colour model.
    /// </summary>
    public abstract class HSVColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly Box background;
        private readonly SaturationValueSelector saturationValueSelector;
        private readonly HueSelector hueSelector;

        protected HSVColourPicker()
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        saturationValueSelector = CreateSaturationValueSelector(),
                        hueSelector = CreateHueSelector()
                    }
                }
            };
        }

        /// <summary>
        /// Creates the control to be used for interactively selecting the hue of the target colour.
        /// </summary>
        protected abstract HueSelector CreateHueSelector();

        /// <summary>
        /// Creates the control to be used for interactively selecting the saturation and value of the target colour.
        /// </summary>
        protected abstract SaturationValueSelector CreateSaturationValueSelector();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            saturationValueSelector.Hue.BindTo(hueSelector.Hue);

            saturationValueSelector.Hue.BindValueChanged(_ => updateCurrent());
            saturationValueSelector.Saturation.BindValueChanged(_ => updateCurrent());
            saturationValueSelector.Value.BindValueChanged(_ => updateCurrent(), true);
        }

        private void updateCurrent()
        {
            Current.Value = Colour4.FromHSV(saturationValueSelector.Hue.Value, saturationValueSelector.Saturation.Value, saturationValueSelector.Value.Value);
        }

        public abstract class SaturationValueSelector : CompositeDrawable
        {
            public Bindable<float> Hue { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            public Bindable<float> Saturation { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            public Bindable<float> Value { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };
        }

        public abstract class HueSelector : CompositeDrawable
        {
            public Bindable<float> Hue { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };
        }
    }
}
