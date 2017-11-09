// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleHover
    {
        /// <summary>
        /// Triggered once when this Drawable becomes hovered.
        /// </summary>
        /// <param name="state">The state at which the Drawable becomes hovered.</param>
        /// <returns>True if this Drawable would like to handle the hover. If so, then
        /// no further Drawables up the scene graph will receive hovering events. If
        /// false, however, then <see cref="IHandleOnHoverLost.OnHoverLost(InputState)"/> will still be
        /// received once hover is lost.</returns>
        bool OnHover(InputState state);

        /// <summary>
        /// Triggered whenever this drawable is no longer hovered.
        /// </summary>
        /// <param name="state">The state at which hover is lost.</param>
        void OnHoverLost(InputState state);
    }
}
