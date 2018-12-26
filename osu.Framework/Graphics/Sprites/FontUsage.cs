// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Represents a specific font and its intended usage, including the font <see cref="Family"/>, <see cref="Weight"/> and whether <see cref="Italics"/> are used.
    /// </summary>
    public readonly struct FontUsage
    {
        /// <summary>
        /// Creates an instance of <see cref="FontUsage"/> using the specified font <paramref name="family"/>, font <paramref name="weight"/> and a value indicating whether the used font is italic or not.
        /// </summary>
        /// <param name="family">The used font family.</param>
        /// <param name="weight">The used font weight.</param>
        /// <param name="italics">Whether the font is italic.</param>
        public FontUsage([NotNull] string family, [CanBeNull] string weight = null, bool italics = false)
        {
            Family = string.IsNullOrEmpty(family) ? throw new ArgumentException("Cannot be null or empty.", nameof(family)) : family;
            Weight = weight;
            Italics = italics;

            FontName = Family + "-";
            if (!string.IsNullOrEmpty(weight))
                FontName += weight;

            if (italics)
                FontName += "Italic";

            FontName = FontName.TrimEnd('-');
        }

        /// <summary>
        /// Gets or sets the font family's name.
        /// </summary>
        [NotNull]
        public string Family { get; }

        /// <summary>
        /// Gets or sets the font's weight.
        /// </summary>
        [CanBeNull]
        public string Weight { get; }

        /// <summary>
        /// Gets or sets whether the font is italic.
        /// </summary>
        public bool Italics { get; }

        /// <summary>
        /// Gets the full font name, based on all other properties.
        /// </summary>
        [NotNull]
        public string FontName { get; }
    }
}
