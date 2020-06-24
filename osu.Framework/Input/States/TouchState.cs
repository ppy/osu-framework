// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Input.States
{
    public class TouchState
    {
        /// <summary>
        /// The maximum amount of touches this can handle.
        /// </summary>
        public static readonly int MAX_TOUCH_COUNT = Enum.GetValues(typeof(TouchSource)).Length;

        /// <summary>
        /// The list of currently active touch sources.
        /// </summary>
        public readonly ButtonStates<TouchSource> ActiveSources = new ButtonStates<TouchSource>();

        /// <summary>
        /// The array to retrieve current touch positions from and set them.
        /// </summary>
        /// <remarks>
        /// Using <see cref="GetTouchPosition"/> is recommended for retrieving
        /// logically correct values, as this may contain already stale values.
        /// </remarks>
        public readonly Vector2[] TouchPositions = new Vector2[MAX_TOUCH_COUNT];

        /// <summary>
        /// Retrieves the current touch position of a specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The touch source.</param>
        /// <returns>The touch position, or null if provided <paramref name="source"/> is not currently active.</returns>
        public Vector2? GetTouchPosition(TouchSource source) => IsActive(source) ? TouchPositions[(int)source] : (Vector2?)null;

        /// <summary>
        /// Whether the provided touch <paramref name="source"/> is active.
        /// </summary>
        /// <param name="source">The touch source to check for.</param>
        public bool IsActive(TouchSource source) => ActiveSources.IsPressed(source);
    }
}
