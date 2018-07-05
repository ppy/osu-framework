// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
