// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Represents a specific usage of an icon.
    /// </summary>
    public readonly struct IconUsage : IEquatable<IconUsage>
    {
        /// <summary>
        /// The font family name.
        /// </summary>
        [CanBeNull]
        public string Family { get; }

        /// <summary>
        /// The font weight.
        /// </summary>
        [CanBeNull]
        public string Weight { get; }

        /// <summary>
        /// The font's full name to be used for lookups. This is an aggregate of all other properties of <see cref="IconUsage"/>.
        /// <remarks>
        /// The format is of the form: <br />
        /// {Family} <br />
        /// {Family}-Italic <br />
        /// {Family}-{Weight}Italic
        /// </remarks>
        /// </summary>
        [NotNull]
        public string FontName { get; }

        /// <summary>
        /// The icon character.
        /// </summary>
        public char Icon { get; }

        /// <summary>
        /// Creates an instance of <see cref="IconUsage"/> using the specified font <paramref name="family"/>, font <paramref name="weight"/> and a value indicating whether the used font is italic or not.
        /// </summary>
        /// /// <param name="icon">The icon.</param>
        /// <param name="family">The font family name.</param>
        /// <param name="weight">The font weight.</param>
        public IconUsage(char icon, [CanBeNull] string family = null, [CanBeNull] string weight = null)
        {
            Icon = icon;
            Family = family;
            Weight = weight;

            FontName = Family + "-";
            if (!string.IsNullOrEmpty(weight))
                FontName += weight;

            FontName = FontName.TrimEnd('-');
        }

        /// <summary>
        /// Creates a new <see cref="IconUsage"/> by applying adjustments to this <see cref="IconUsage"/>.
        /// </summary>
        /// <param name="family">The font family. If null, the value is copied from this <see cref="IconUsage"/>.</param>
        /// <param name="weight">The font weight. If null, the value is copied from this <see cref="IconUsage"/>.</param>
        /// <returns>The resulting <see cref="IconUsage"/>.</returns>
        public IconUsage With([CanBeNull] string family = null, [CanBeNull] string weight = null)
            => new IconUsage(Icon, family ?? Family, weight ?? Weight);

        public override string ToString() => $"Icon={Icon} Font={FontName}";

        public bool Equals(IconUsage other) => Icon == other.Icon && Family == other.Family && Weight == other.Weight;

        public override bool Equals(object obj) => obj is IconUsage other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Family, Icon, Weight);
    }
}
