// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osuTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Represents a touch structure that provides a touch source and current position.
    /// </summary>
    public readonly struct Touch : IEquatable<Touch>
    {
        /// <summary>
        /// The source of this touch.
        /// </summary>
        public readonly TouchSource Source;

        /// <summary>
        /// The current position of this touch.
        /// </summary>
        public readonly Vector2 Position;

        public Touch(TouchSource source, Vector2 position)
        {
            Source = source;
            Position = position;
        }

        /// <summary>
        /// Indicates whether the <see cref="Source"/> of this touch is equal to <see cref="Source"/> of the other touch.
        /// </summary>
        /// <param name="other">The other touch.</param>
        public bool Equals(Touch other) => Source == other.Source;

        public static bool operator ==(Touch left, Touch right) => left.Equals(right);
        public static bool operator !=(Touch left, Touch right) => !(left == right);

        public override bool Equals(object obj) => obj is Touch other && Equals(other);

        public override int GetHashCode() => Source.GetHashCode();
    }
}
