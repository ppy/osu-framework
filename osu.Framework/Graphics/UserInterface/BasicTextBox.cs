// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicTextBox : TextBox
    {
        protected override float CaretWidth => 2;

        protected override Color4 SelectionColour => FrameworkColour.YellowGreen;

        public BasicTextBox()
        {
            CornerRadius = 0;
            BackgroundFocused = FrameworkColour.BlueGreen;
            BackgroundUnfocused = FrameworkColour.BlueGreenDark;
            BackgroundCommit = FrameworkColour.Green;
            TextFlow.Height = 0.75f;
        }

        protected override Drawable GetDrawableCharacter(char c) => new SpriteText { Text = c.ToString(), Font = FrameworkFont.Condensed.With(size: CalculatedTextSize) };

        protected override SpriteText CreatePlaceholder() => new SpriteText
        {
            Colour = FrameworkColour.YellowGreen,
            Font = FrameworkFont.Condensed,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
        };
    }
}
