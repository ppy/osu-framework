// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class BasicSwatchColourPicker : SwatchColourPicker
    {
        private const float swatch_size = 28;

        public BasicSwatchColourPicker()
        {
            Background.Colour = FrameworkColour.GreenDarker;
            Content.Padding = new MarginPadding { Horizontal = 20, Top = 20 };
        }

        protected override ClickableContainer CreateSwatch(Colour4 colour) => new BasicSwatch(colour);

        private partial class BasicSwatch : ClickableContainer
        {
            public BasicSwatch(Colour4 colour)
            {
                Size = new Vector2(swatch_size);
                Masking = true;
                CornerRadius = 5;

                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colour,
                };
            }
        }
    }
}
