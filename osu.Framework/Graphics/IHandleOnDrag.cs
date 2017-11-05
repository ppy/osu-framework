// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnDrag
    {
        /// <summary>
        /// Triggered whenever the mouse is moved while dragging.
        /// Only is received if a drag was previously initiated by returning true
        /// from <see cref="IHandleOnDragStart.OnDragStart(InputState)"/>.
        /// </summary>
        /// <param name="state">The state after the mouse was moved.</param>
        /// <returns>Currently unused.</returns>
        bool OnDrag(InputState state);
    }
}
