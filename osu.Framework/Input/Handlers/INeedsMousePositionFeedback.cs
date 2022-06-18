// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;

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
        /// <param name="isSelfFeedback">Whether the feedback was triggered from this handler.</param>
        void FeedbackMousePositionChange(Vector2 position, bool isSelfFeedback);
    }
}
