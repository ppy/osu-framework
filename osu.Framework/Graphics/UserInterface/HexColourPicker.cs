// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class HexColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly Box background;
        private readonly TextBox hexCodeTextBox;
        private readonly Drawable spacer;
        private readonly ColourPreview colourPreview;

        protected HexColourPicker()
        {
            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                new GridContainer
                {
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension()
                    },
                    Content = new[]
                    {
                        new[]
                        {
                            hexCodeTextBox = CreateHexCodeTextBox(),
                            spacer = Empty(),
                            colourPreview = CreateColourPreview()
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates the text box to be used for specifying the hex code of the target colour.
        /// </summary>
        protected abstract TextBox CreateHexCodeTextBox();

        /// <summary>
        /// Creates the control that will be used for displaying the preview of the target colour.
        /// </summary>
        protected abstract ColourPreview CreateColourPreview();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            colourPreview.Current.BindTo(Current);
        }

        public abstract class ColourPreview : CompositeDrawable
        {
            public Bindable<Colour4> Current = new Bindable<Colour4>();
        }
    }
}
