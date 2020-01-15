// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
        /// The dictionary to retrieve current touch positions from and save them.
        /// The values in this dictionary remain the same regardless of any touch activity change, use <see cref="GetTouchPosition"/> instead.
        /// </summary>
        public readonly Dictionary<MouseButton, Vector2> TouchPositions = new Dictionary<MouseButton, Vector2>();

        /// <summary>
        /// Retrieves the current touch position of a specified <paramref name="source"/>, or null if not active nor existing in the <see cref="TouchPositions"/> dictionary.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2? GetTouchPosition(MouseButton source)
        {
            if (!IsActive(source) || !TouchPositions.TryGetValue(source, out var pos))
                return null;

            return pos;
        }

        public bool IsActive(MouseButton source) => ActiveSources.IsPressed(source);
    }
}
