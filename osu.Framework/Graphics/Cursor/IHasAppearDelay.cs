// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// A tooltip which provides a custom delay until it appears, override the <see cref="TooltipContainer"/>-wide default.
    /// </summary>
    public interface IHasAppearDelay : IHasTooltip
    {
        /// <summary>
        /// The delay until the tooltip should be displayed.
        /// </summary>
        double AppearDelay { get; }
    }
}
