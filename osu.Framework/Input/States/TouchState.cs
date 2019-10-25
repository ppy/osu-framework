// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// Retrieves the recent pointer of all active pointers.
        /// Null is returned when there are no active pointers.
        /// </summary>
        public PositionalPointer? PrimaryPointer => Pointers.Any() ? (PositionalPointer?)Pointers.OrderByDescending(p => p.Source).First() : null;
    }
}
