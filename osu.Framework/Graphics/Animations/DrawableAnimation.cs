// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation that switches the displayed drawable when a new frame is displayed.
    /// </summary>
    public class DrawableAnimation : Animation<Drawable>
    {
        protected override void DisplayFrame(Drawable content)
        {
            Clear(false);
            Add(content);
        }
    }
}
