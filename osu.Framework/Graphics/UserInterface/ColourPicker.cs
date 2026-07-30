// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A group of controls to be used for selecting a colour.
    /// Allows both for mouse-interactive input (via <see cref="HSVColourPicker"/>) and textual input (via <see cref="HexColourPicker"/>),
    /// with an optional preset swatch row (via <see cref="SwatchColourPicker"/>).
    /// </summary>
    public abstract partial class ColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private HSVColourPicker hsvColourPicker = null!;
        private HexColourPicker hexColourPicker = null!;
        private SwatchColourPicker? swatchColourPicker;

        protected ColourPicker()
        {
            Current.Value = Colour4.White;
            AutoSizeAxes = Axes.Y;
            Width = 300;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            hsvColourPicker = CreateHSVColourPicker().With(d =>
            {
                d.RelativeSizeAxes = Axes.X;
                d.Width = 1;
            });

            swatchColourPicker = CreateSwatchColourPicker()?.With(d =>
            {
                d.RelativeSizeAxes = Axes.X;
                d.Width = 1;
            });

            hexColourPicker = CreateHexColourPicker().With(d =>
            {
                d.RelativeSizeAxes = Axes.X;
                d.Width = 1;
            });

            var children = new List<Drawable> { hsvColourPicker };

            if (swatchColourPicker != null)
                children.Add(swatchColourPicker);

            children.Add(hexColourPicker);

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = children
            };
        }

        /// <summary>
        /// Creates the control that allows for interactively specifying the target colour, using the hue-saturation-value colour model.
        /// </summary>
        protected abstract HSVColourPicker CreateHSVColourPicker();

        /// <summary>
        /// Creates the optional control that shows clickable colour presets.
        /// </summary>
        protected virtual SwatchColourPicker? CreateSwatchColourPicker() => null;

        /// <summary>
        /// Creates the control that allows for specifying the target colour using a hex code.
        /// </summary>
        protected abstract HexColourPicker CreateHexColourPicker();

        public override bool IsPresent => base.IsPresent || hsvColourPicker.IsPresent;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            hsvColourPicker.Current = Current;

            if (swatchColourPicker != null)
                swatchColourPicker.Current = Current;

            hexColourPicker.Current = Current;
        }
    }
}
