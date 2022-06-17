// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        public new MarginPadding Padding
        {
            get => content.Padding;
            set => content.Padding = value;
        }

        /// <summary>
        /// Sets the spacing between the hex input text box and the colour preview.
        /// </summary>
        public float Spacing
        {
            get => spacer.Width;
            set => spacer.Width = value;
        }

        /// <summary>
        /// The background of the control.
        /// </summary>
        protected readonly Box Background;

        private readonly Container content;

        private readonly TextBox hexCodeTextBox;
        private readonly Drawable spacer;
        private readonly ColourPreview colourPreview;

        protected HexColourPicker()
        {
            Current.Value = Colour4.White;

            Width = 300;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                content = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension()
                        },
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize)
                        },
                        Content = new[]
                        {
                            new[]
                            {
                                hexCodeTextBox = CreateHexCodeTextBox().With(d =>
                                {
                                    d.RelativeSizeAxes = Axes.X;
                                    d.CommitOnFocusLost = true;
                                }),
                                spacer = Empty(),
                                colourPreview = CreateColourPreview().With(d => d.RelativeSizeAxes = Axes.Both)
                            }
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

            Current.BindValueChanged(_ => updateState(), true);

            hexCodeTextBox.Current.BindValueChanged(_ => tryPreviewColour());
            hexCodeTextBox.OnCommit += commitColour;
        }

        private void updateState()
        {
            hexCodeTextBox.Text = Current.Value.ToHex();
            colourPreview.Current.Value = Current.Value;
        }

        private void tryPreviewColour()
        {
            if (!Colour4.TryParseHex(hexCodeTextBox.Text, out var colour) || colour.A < 1)
                return;

            colourPreview.Current.Value = colour;
        }

        private void commitColour(TextBox sender, bool newText)
        {
            if (!Colour4.TryParseHex(sender.Text, out var colour) || colour.A < 1)
            {
                Current.TriggerChange(); // restore previous value.
                return;
            }

            Current.Value = colour;
        }

        public abstract class ColourPreview : CompositeDrawable
        {
            public Bindable<Colour4> Current = new Bindable<Colour4>();
        }
    }
}
