// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using OpenTK;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopGameWindow : BasicGameWindow
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        public DesktopGameWindow() : base(default_width, default_height)
        {
        }

        public override void CentreToScreen()
        {
            base.CentreToScreen();
            Location = new System.Drawing.Point(
                (DisplayDevice.Default.Width - Size.Width) / 2,
                (DisplayDevice.Default.Height - Size.Height) / 2
            );
        }
    }
}
