// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicPasswordTextBox : BasicTextBox, ISuppressKeyEventLogging
    {
        protected virtual char MaskCharacter => '*';

        protected override bool AllowClipboardExport => false;

        protected override bool AllowWordNavigation => false;

        protected override bool AllowIme => false;

        protected override Drawable AddCharacterToFlow(char c) => base.AddCharacterToFlow(MaskCharacter);
    }
}
