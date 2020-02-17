// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an absolute change of a provided touch source's position.
    /// Any provided touch source should always be in the range <see cref="MouseButton.Touch1"/>-<see cref="MouseButton.Touch10"/>.
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
            if (source < MouseButton.Touch1 || source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {source}", nameof(source));

            Source = source;
            Position = newPosition;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touch = state.Touch;

            Vector2? lastPosition = touch.GetTouchPosition(Source);
            if (lastPosition == Position)
                return;

            touch.TouchPositions[Source] = Position;
            handler.HandleInputStateChange(new TouchPositionChangeEvent(state, this, Source, lastPosition ?? Position));
        }
    }
}
