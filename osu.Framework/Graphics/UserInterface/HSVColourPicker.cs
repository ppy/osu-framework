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
    public abstract partial class HSVColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        /// <summary>
        /// When set, colour changes based on user input are only transferred to any bound <see cref="Current"/> on commit.
        /// This is useful if the UI interaction could be adversely affected by the value changing rapidly, such as in the case of
        /// the <see cref="SaturationValueSelector"/>'s or <see cref="HueSelector"/>'s interactive elements being moved by the user.
        /// </summary>
        public bool TransferValueOnCommit { get; set; }

        /// <summary>
        /// The background of the control.
        /// </summary>
        protected Box Background { get; }

        /// <summary>
        /// Contains the elements of the colour picker.
        /// </summary>
        protected FillFlowContainer Content { get; }

        private readonly Bindable<Colour4> currentInstantaneous = new Bindable<Colour4>();

        private readonly SaturationValueSelector saturationValueSelector;
        private readonly HueSelector hueSelector;

        protected HSVColourPicker()
        {
            Width = 300;
            AutoSizeAxes = Axes.Y;
            Current.Value = Colour4.White;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                Content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
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

            saturationValueSelector.Current.BindTo(currentInstantaneous);
            hueSelector.Hue.BindTo(saturationValueSelector.Hue);

            current.BindValueChanged(colour => currentInstantaneous.Value = colour.NewValue, true);
            currentInstantaneous.BindValueChanged(colour =>
            {
                if (!TransferValueOnCommit)
                    Current.Value = colour.NewValue;
            });

            saturationValueSelector.OnCommit += instantaneousValueCommitted;
            hueSelector.OnCommit += instantaneousValueCommitted;
        }

        private void instantaneousValueCommitted() => current.Value = currentInstantaneous.Value;
    }
}
