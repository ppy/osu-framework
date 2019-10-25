// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Represents a positional pointer.
    /// </summary>
    public struct PositionalPointer : IEquatable<PositionalPointer>
    {
        /// <summary>
        /// Source of the pointer. (required for touch pointers)
        /// </summary>
        public readonly MouseButton Source;

        /// <summary>
        /// Position of the pointer.
        /// </summary>
        public Vector2 Position;

        public PositionalPointer(MouseButton source, Vector2 position)
        {
            Source = source;
            Position = position;
        }

        public bool Equals(PositionalPointer other) => Source == other.Source;
    }
}
