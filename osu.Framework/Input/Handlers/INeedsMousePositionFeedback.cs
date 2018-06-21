// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input.Handlers
{
    /// <summary>
    /// An <see cref="InputHandler"/> which needs to receive feedback of the final mouse position.
    /// </summary>
    public interface INeedsMousePositionFeedback
    {
        /// <summary>
        /// Receives the final mouse position from an <see cref="InputManager"/>.
        /// </summary>
        /// <param name="position">The final mouse position.</param>
        void FeedbackMousePositionChange(Vector2 position);
    }
}
