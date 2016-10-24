// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using osu.Framework.Desktop.Input;
using osu.Framework.Input;
using osu.Framework.Platform;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopGameWindow : BasicGameWindow
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        public DesktopGameWindow() : base(default_width, default_height)
        {
        }

        internal TextInputSource CreateTextInput() => null;// new BackingTextBox(Form);
    }
}
