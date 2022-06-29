// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// The background of the control.
        /// </summary>
        protected Box Background { get; }

        /// <summary>
        /// Contains the elements of the colour picker.
        /// </summary>
        protected FillFlowContainer Content { get; }

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

        public override bool IsPresent => base.IsPresent
                                          || saturationValueSelector.Scheduler.HasPendingTasks
                                          || hueSelector.Scheduler.HasPendingTasks;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            saturationValueSelector.Current.BindTo(current);
            hueSelector.Hue.BindTo(saturationValueSelector.Hue);
        }
    }
}
