// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A row of clickable colour presets for use in a <see cref="ColourPicker"/>.
    /// Hidden when <see cref="Colours"/> is empty.
    /// </summary>
    public abstract partial class SwatchColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        /// <summary>
        /// The preset colours to display as swatches.
        /// </summary>
        public BindableList<Colour4> Colours { get; }

        /// <summary>
        /// The background of the control.
        /// </summary>
        protected Box Background { get; }

        /// <summary>
        /// Contains the swatch drawables.
        /// </summary>
        protected FillFlowContainer Content { get; }

        protected SwatchColourPicker(BindableList<Colour4>? colours = null)
        {
            Colours = colours ?? new BindableList<Colour4>();

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

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
                    Direction = FillDirection.Full,
                    Spacing = new Vector2(5),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                }
            };
        }

        public override bool IsPresent => base.IsPresent && Colours.Count > 0;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Colours.BindCollectionChanged((_, _) => updateSwatches(), true);
        }

        private void updateSwatches()
        {
            Content.Clear(true);

            foreach (var colour in Colours)
            {
                var swatch = CreateSwatch(colour).With(s => s.Anchor = s.Origin = Anchor.TopCentre);
                swatch.Action = () => Current.Value = colour;
                Content.Add(swatch);
            }
        }

        /// <summary>
        /// Creates a clickable swatch for the given colour.
        /// </summary>
        protected abstract ClickableContainer CreateSwatch(Colour4 colour);
    }
}
