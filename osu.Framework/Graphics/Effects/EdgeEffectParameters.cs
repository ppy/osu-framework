// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// Parametrizes the appearance of an edge effect.
    /// </summary>
    public struct EdgeEffectParameters : IEquatable<EdgeEffectParameters>
    {
        /// <summary>
        /// Colour of the edge effect.
        /// </summary>
        public SRGBColour Colour;

        /// <summary>
        /// Positional offset applied to the edge effect.
        /// Useful for off-center shadows.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// The type of the edge effect.
        /// </summary>
        public EdgeEffectType Type;

        /// <summary>
        /// How round the edge effect should appear. Adds to the <see cref="CompositeDrawable.CornerRadius"/>
        /// of the corresponding <see cref="CompositeDrawable"/>. Not to confuse with the <see cref="Radius"/>.
        /// </summary>
        public float Roundness;

        /// <summary>
        /// How "thick" the edge effect is around the <see cref="CompositeDrawable"/>. In other words: At what distance
        /// from the <see cref="CompositeDrawable"/>'s border the edge effect becomes fully invisible.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Whether the inside of the EdgeEffect rectangle should be empty.
        /// </summary>
        public bool Hollow;

        public readonly bool Equals(EdgeEffectParameters other) =>
            Colour.Equals(other.Colour) &&
            Offset == other.Offset &&
            Type == other.Type &&
            Roundness == other.Roundness &&
            Radius == other.Radius;

        public override readonly string ToString() => Type != EdgeEffectType.None ? $@"{Radius} {Type}EdgeEffect" : @"EdgeEffect (Disabled)";
    }
}
