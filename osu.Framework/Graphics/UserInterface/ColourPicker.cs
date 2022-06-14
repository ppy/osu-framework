// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A group of controls to be used for selecting a colour.
    /// Allows both for mouse-interactive input (via <see cref="HSVColourPicker"/>) and textual input (via <see cref="HexColourPicker"/>).
    /// </summary>
    public abstract class ColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly HSVColourPicker hsvColourPicker;
        private readonly HexColourPicker hexColourPicker;

        protected ColourPicker()
        {
            Current.Value = Colour4.White;
            AutoSizeAxes = Axes.Y;
            Width = 300;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        hsvColourPicker = CreateHSVColourPicker().With(d =>
                        {
                            d.RelativeSizeAxes = Axes.X;
                            d.Width = 1;
                        }),
                        hexColourPicker = CreateHexColourPicker().With(d =>
                        {
                            d.RelativeSizeAxes = Axes.X;
                            d.Width = 1;
                        })
                    }
                }
            };
        }

        /// <summary>
        /// Creates the control that allows for interactively specifying the target colour, using the hue-saturation-value colour model.
        /// </summary>
        protected abstract HSVColourPicker CreateHSVColourPicker();

        /// <summary>
        /// Creates the control that allows for specifying the target colour using a hex code.
        /// </summary>
        protected abstract HexColourPicker CreateHexColourPicker();

        public override bool IsPresent => base.IsPresent || hsvColourPicker.IsPresent;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            hsvColourPicker.Current = Current;
            hexColourPicker.Current = Current;
        }
    }
}
