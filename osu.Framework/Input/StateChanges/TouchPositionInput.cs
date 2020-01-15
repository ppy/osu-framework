// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an absolute change of a provided touch <see cref="Source"/>'s position.
    /// Any provided touch source should always be in the range <see cref="MouseButton.Touch1"/> - <see cref="MouseButton.Touch10"/>.
    /// </summary>
    public class TouchPositionInput : IInput
    {
        /// <summary>
        /// The touch source to be modified.
        /// </summary>
        public readonly MouseButton Source;

        /// <summary>
        /// The new position to move to.
        /// </summary>
        public readonly Vector2 Position;

        public TouchPositionInput(MouseButton source, Vector2 newPosition)
        {
            Source = source;
            Position = newPosition;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touch = state.Touch;

            if (Source < MouseButton.Touch1)
                return;

            Vector2? lastPosition = touch.GetTouchPosition(Source);
            if (lastPosition == Position)
                return;

            touch.TouchPositions[Source] = Position;
            handler.HandleInputStateChange(new TouchPositionChangeEvent(state, this, Source, lastPosition ?? Position));
        }
    }
}
