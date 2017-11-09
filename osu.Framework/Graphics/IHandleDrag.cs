// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleDrag
    {
        /// <summary>
        /// Triggered whenever this Drawable is initially dragged by a held mouse click
        /// and subsequent movement.
        /// </summary>
        /// <param name="state">The state after the mouse was moved.</param>
        /// <returns>True if this Drawable accepts being dragged. If so, then future
        /// <see cref="IHandleOnDrag.OnDrag(InputState)"/> and <see cref="IHandleOnDragEnd.OnDragEnd(InputState)"/>
        /// events will be received. Otherwise, the event is propagated up the scene
        /// graph to the next eligible Drawable.</returns>
        bool OnDragStart(InputState state);

        /// <summary>
        /// Triggered whenever the mouse is moved while dragging.
        /// Only is received if a drag was previously initiated by returning true
        /// from <see cref="IHandleOnDragStart.OnDragStart(InputState)"/>.
        /// </summary>
        /// <param name="state">The state after the mouse was moved.</param>
        /// <returns>Currently unused.</returns>
        bool OnDrag(InputState state);

        /// <summary>
        /// Triggered whenever a drag ended. Only is received if a drag was previously
        /// initiated by returning true from <see cref="IHandleOnDragStart.OnDragStart(InputState)"/>.
        /// </summary>
        /// <param name="state">The state after the drag ended.</param>
        /// <returns>Currently unused.</returns>
        bool OnDragEnd(InputState state);
    }
}
