// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.UserInterface
{
    public class PasswordTextBox : TextBox
    {
        protected virtual char MaskCharacter => '*';

        protected override bool AllowClipboardExport => false;

        protected override bool AllowWordNavigation => false;

        protected override Drawable AddCharacterToFlow(char c) => base.AddCharacterToFlow(MaskCharacter);
    }
}
