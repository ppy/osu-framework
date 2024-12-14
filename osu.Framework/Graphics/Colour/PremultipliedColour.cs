// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// Represents a <see cref="Color4"/> provided in premultiplied-alpha form.
    /// </summary>
    public readonly struct PremultipliedColour : IEquatable<PremultipliedColour>
    {
        /// <summary>
        /// The <see cref="Color4"/> after alpha multiplication.
        /// </summary>
        public readonly Color4 Premultiplied;

        private PremultipliedColour(Color4 premultiplied)
        {
            Premultiplied = premultiplied;
        }

        /// <summary>
        /// Creates a <see cref="PremultipliedColour"/> from a straight-alpha colour.
        /// </summary>
        /// <param name="colour">The straight-alpha colour.</param>
        public static PremultipliedColour FromStraight(Color4 colour)
        {
            colour.R *= colour.A;
            colour.G *= colour.A;
            colour.B *= colour.A;
            return new PremultipliedColour(colour);
        }

        /// <summary>
        /// Creates a <see cref="PremultipliedColour"/> from a premultiplied-alpha colour.
        /// </summary>
        /// <param name="colour">The premultiplied-alpha colour.</param>
        public static PremultipliedColour FromPremultiplied(Color4 colour) => new PremultipliedColour(colour);

        public bool Equals(PremultipliedColour other) => Premultiplied.Equals(other.Premultiplied);
    }
}
