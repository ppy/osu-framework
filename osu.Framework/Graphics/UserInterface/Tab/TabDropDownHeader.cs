// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.UserInterface.Tab
{
    public abstract class TabDropDownHeader : DropDownHeader
    {
        protected TabDropDownHeader()
        {
            Background.Hide(); // don't need a background
            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.Both;
            Foreground.RelativeSizeAxes = Axes.None;
            Foreground.AutoSizeAxes = Axes.Both;
        }
    }
}
