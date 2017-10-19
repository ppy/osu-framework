// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F4 && e.Alt)
            {
                Exit();
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
