// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

namespace osu.Framework.Input.States
{
    public class TouchState
    {
        /// <summary>
        /// Represents a list of currently active pointers.
        /// </summary>
        public readonly ButtonStates<PositionalPointer> Pointers = new ButtonStates<PositionalPointer>();

        /// <summary>
        /// Attempts to retrieve the recent pointer of all active pointers.
        /// </summary>
        /// <returns>Whether the pointer is successfully retrieved.</returns>
        public bool TryGetPrimaryPointer(out PositionalPointer primary)
        {
            try
            {
                primary = Pointers.OrderByDescending(p => p.Source).First();
                return true;
            }
            catch (InvalidOperationException)
            {
                primary = default;
                return false;
            }
        }
    }
}
