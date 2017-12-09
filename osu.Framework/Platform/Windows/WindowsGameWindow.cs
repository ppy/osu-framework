// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        public WindowsGameWindow()
        {
            Implementation.KeyDown += OnKeyDown;
        }

        protected void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F4 && e.Alt)
                Implementation.Exit();
        }
    }
}
