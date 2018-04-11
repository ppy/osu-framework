// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// Implementing this interface allows the implementing <see cref="Drawable"/> to display a custom tooltip if it is the child of a <see cref="TooltipContainer"/>.
    /// Keep in mind that tooltips can only be displayed by a <see cref="TooltipContainer"/> if the <see cref="Drawable"/> implementing <see cref="IHasCustomTooltip"/> has <see cref="Drawable.HandleMouseInput"/> set to true.
    /// </summary>
    public interface IHasCustomTooltip : IHasTooltip
    {
        /// <summary>
        /// The custom tooltip that should be displayed.
        /// </summary>
        /// <returns>The custom tooltip that should be displayed.</returns>
        ITooltip GetCustomTooltip();
    }
}
