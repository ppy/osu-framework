// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Represents a specific font and its intended usage, including the font <see cref="Family"/>, <see cref="Weight"/> and whether <see cref="Italics"/> are used.
    /// </summary>
    public class FontUsage
    {
        private string family;
        private string weight;
        private bool italics;

        /// <summary>
        /// Creates an instance of <see cref="FontUsage"/> using the default font family (OpenSans).
        /// </summary>
        public FontUsage()
        {
            family = "OpenSans";
            updateFontName();
        }

        /// <summary>
        /// Gets or sets the font family's name.
        /// </summary>
        [NotNull]
        public string Family
        {
            get => family;
            set
            {
                if (family == value)
                    return;

                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Cannot be null or empty.", nameof(Family));

                family = value;
                updateFontName();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Gets or sets the font's weight.
        /// </summary>
        [CanBeNull]
        public string Weight
        {
            get => weight;
            set
            {
                if (weight == value)
                    return;

                weight = value;
                updateFontName();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Gets or sets whether the font is italic.
        /// </summary>
        public bool Italics
        {
            get => italics;
            set
            {
                if (italics == value)
                    return;

                italics = value;
                updateFontName();
                Changed?.Invoke();
            }
        }

        private void updateFontName()
        {
            if (string.IsNullOrEmpty(weight) && !italics)
            {
                FontName = Family;
                return;
            }

            string result = Family + "-";

            if (!string.IsNullOrEmpty(weight))
                result += weight;

            if (italics)
                result += "Italic";

            FontName = result;
        }

        /// <summary>
        /// Gets the full font name, based on all other properties.
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Represents a change in any font property.
        /// This can lead to a change in character texture and/or layout.
        /// </summary>
        public event Action Changed;
    }
}
