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
        private const float default_text_size = 20;

        /// <summary>
        /// Creates an instance of <see cref="FontUsage"/> using the specified font <paramref name="family"/>, font <paramref name="weight"/> and a value indicating whether the used font is italic or not.
        /// </summary>
        /// <param name="family">The used font family.</param>
        /// <param name="size">The used text size in local space.</param>
        /// <param name="weight">The used font weight.</param>
        /// <param name="italics">Whether the font is italic.</param>
        /// <param name="fixedWidth">Whether all characters should be spaced apart the same distance.</param>
        public FontUsage([NotNull] string family, float size = default_text_size, [CanBeNull] string weight = null, bool italics = false, bool fixedWidth = false)
        {
            Family = string.IsNullOrEmpty(family) ? throw new ArgumentException("Cannot be null or empty.", nameof(family)) : family;
            Size = size >= 0 ? size : throw new ArgumentOutOfRangeException(nameof(size), "Must be non-negative.");
            Weight = weight;
            Italics = italics;
            FixedWidth = fixedWidth;

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
        /// Gets the size of the text in local space. This means that if <see cref="Size"/> is set to 16, a single line will have a height of 16.
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// Gets whether all characters should be spaced apart the same distance.
        /// </summary>
        public bool FixedWidth { get; }

        /// <summary>
        /// Gets the full font name, based on all other properties.
        /// </summary>
        [NotNull]
        public string FontName { get; }
    }
}
