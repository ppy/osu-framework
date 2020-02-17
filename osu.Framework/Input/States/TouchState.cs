// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.States
{
    public class TouchState
    {
        /// <summary>
        /// The list of currently active touch sources.
        /// </summary>
        public readonly ButtonStates<MouseButton> ActiveSources = new ButtonStates<MouseButton>();

        /// <summary>
        /// The array to retrieve current touch positions from and save them.
        /// </summary>
        public readonly Vector2?[] TouchPositions = new Vector2?[10];

        /// <summary>
        /// Retrieves the current touch position of a specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The touch source, null if provided <paramref name="source"/> is not active.</param>
        public Vector2? GetTouchPosition(MouseButton source)
        {
            if (source < MouseButton.Touch1 || source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {source}", nameof(source));

            if (!IsActive(source))
                return null;

            return TouchPositions[source - MouseButton.Touch1];
        }

        /// <summary>
        /// Whether the provided touch <paramref name="source"/> is active.
        /// </summary>
        /// <param name="source">The touch source to check for.</param>
        public bool IsActive(MouseButton source)
        {
            if (source < MouseButton.Touch1 || source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {source}", nameof(source));

            return ActiveSources.IsPressed(source);
        }
    }
}
