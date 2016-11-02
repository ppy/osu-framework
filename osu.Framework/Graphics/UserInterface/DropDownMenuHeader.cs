// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    class DropDownMenuHeader : DropDownMenuItem
    {
        public int Level;

        protected override Color4 BackgroundColour => Color4.DarkBlue;
        protected override Color4 BackgroundColourHover => Color4.DarkBlue;

        public DropDownMenuHeader(DropDownMenu parent) : base(parent)
        {
        }
    }
}
