// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class BasicDropDownHeader : ClickableContainer
    {
        protected internal abstract string Label { get; set; }
    }
}
