// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Objects implementing this interface have a line base height when used in a CustomizableTextContainer.
    /// </summary>
    public interface IHasLineBaseHeight
    {
        /// <summary>
        /// The line base height this object has.
        /// </summary>
        float LineBaseHeight { get; }
    }
}
