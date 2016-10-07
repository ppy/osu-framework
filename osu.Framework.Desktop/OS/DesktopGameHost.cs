// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.OS;

namespace osu.Framework.Desktop.OS
{
    public abstract class DesktopGameHost : BasicGameHost
    {
        public override GLControl GLControl => Window?.Form;

        private TextInputSource textInputBox;
        public override TextInputSource TextInput => textInputBox ?? (textInputBox = ((DesktopGameWindow)Window).CreateTextInput());
    }
}
