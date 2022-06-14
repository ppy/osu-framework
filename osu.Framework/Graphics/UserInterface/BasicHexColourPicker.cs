// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicHexColourPicker : HexColourPicker
    {
        public BasicHexColourPicker()
        {
            Background.Colour = FrameworkColour.GreenDarker;

            Padding = new MarginPadding(20);
            Spacing = 10;
        }

        protected override TextBox CreateHexCodeTextBox() => new BasicTextBox
        {
            Height = 40
        };

        protected override ColourPreview CreateColourPreview() => new BasicColourPreview();

        private class BasicColourPreview : ColourPreview
        {
            private readonly Box previewBox;

            public BasicColourPreview()
            {
                InternalChild = previewBox = new Box
                {
                    RelativeSizeAxes = Axes.Both
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(_ => updatePreview(), true);
            }

            private void updatePreview()
            {
                previewBox.Colour = Current.Value;
            }
        }
    }
}
