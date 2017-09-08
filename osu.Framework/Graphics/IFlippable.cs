// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Interface for components that can be flipped horizontally or vertically.
    /// </summary>
    public interface IFlippable
    {
        /// <summary>
        /// True if the component should be flipped horizontally.
        /// </summary>
        bool FlipH { get; set; }

        /// <summary>
        /// True if the component should be flipped vertically.
        /// </summary>
        bool FlipV { get; set; }
    }
}
