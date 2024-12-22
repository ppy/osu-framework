// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// Represents a structure for containing a "premultiplied colour".
    /// </summary>
    public readonly struct PremultipliedColour : IEquatable<PremultipliedColour>
    {
        /// <summary>
        /// The red component of this colour multiplied by the value of <see cref="Occlusion"/>.
        /// </summary>
        public readonly float PremultipliedR;

        /// <summary>
        /// The green component of this colour multiplied by the value of <see cref="Occlusion"/>.
        /// </summary>
        public readonly float PremultipliedG;

        /// <summary>
        /// The blue component of this colour multiplied by the value of <see cref="Occlusion"/>.
        /// </summary>
        public readonly float PremultipliedB;

        /// <summary>
        /// The alpha component of this colour, often referred to as "occlusion" instead of "opacity" in the context of premultiplied colours.
        /// </summary>
        public readonly float Occlusion;

        public PremultipliedColour(float premultipliedR, float premultipliedG, float premultipliedB, float occlusion)
        {
            PremultipliedR = premultipliedR;
            PremultipliedG = premultipliedG;
            PremultipliedB = premultipliedB;
            Occlusion = occlusion;
        }

        /// <summary>
        /// Creates a <see cref="Rgba32"/> containing the premultiplied components of this colour.
        /// </summary>
        public Rgba32 ToPremultipliedRgba32() => new Rgba32(PremultipliedR, PremultipliedG, PremultipliedB, Occlusion);

        /// <summary>
        /// Creates a <see cref="PremultipliedColour"/> from a straight-alpha colour.
        /// </summary>
        /// <param name="colour">The straight-alpha colour.</param>
        public static PremultipliedColour FromStraight(Color4 colour) => new PremultipliedColour(
            colour.R * colour.A,
            colour.G * colour.A,
            colour.B * colour.A,
            colour.A);

        /// <summary>
        /// Creates a <see cref="PremultipliedColour"/> from a premultiplied-alpha <see cref="Rgba32"/> colour.
        /// </summary>
        /// <param name="premultipliedColour">The premultiplied-alpha <see cref="Rgba32"/> colour.</param>
        public static PremultipliedColour FromPremultiplied(Rgba32 premultipliedColour)
        {
            var premultipliedVector = premultipliedColour.ToVector4();
            return new PremultipliedColour(premultipliedVector.X, premultipliedVector.Y, premultipliedVector.Z, premultipliedVector.W);
        }

        public bool Equals(PremultipliedColour other)
            => PremultipliedR == other.PremultipliedR &&
               PremultipliedG == other.PremultipliedG &&
               PremultipliedB == other.PremultipliedB &&
               Occlusion == other.Occlusion;
    }
}
