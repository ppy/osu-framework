// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisedString : IEquatable<LocalisedString>
    {
        /// <summary>
        /// The text to be localised.
        /// </summary>
        public readonly (string Original, string Fallback) Text;

        /// <summary>
        /// The arguments to format <see cref="Text"/> with.
        /// </summary>
        public readonly object[] Args;

        /// <summary>
        /// Whether <see cref="Text"/> should be localised.
        /// </summary>
        internal readonly bool ShouldLocalise;

        /// <summary>
        /// Creates a new <see cref="LocalisedString"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// </summary>
        /// <param name="text">The text to be localised.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisedString(string text, params object[] args)
            : this((text, text), true, args)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LocalisedString"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// </summary>
        /// <param name="text">The text to be localised. Accepts a fallback value which is used when <see cref="FrameworkSetting.ShowUnicode"/> is false.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisedString((string original, string fallback) text, params object[] args)
            : this(text, true, args)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LocalisedString"/>.
        /// </summary>
        /// <param name="text">The text to use when <see cref="FrameworkSetting.ShowUnicode"/> is true.</param>
        /// <param name="shouldLocalise">Whether the text should be localised.</param>
        /// <param name="args">The arguments to format the text with.</param>
        private LocalisedString((string original, string fallback) text, bool shouldLocalise, params object[] args)
        {
            if (string.IsNullOrEmpty(text.original))
                text.original = text.fallback ?? string.Empty;
            if (string.IsNullOrEmpty(text.fallback))
                text.fallback = text.original;

            Text = text;
            ShouldLocalise = shouldLocalise;
            Args = args;
        }

        public static implicit operator string(LocalisedString localised) => localised.Text.Original;

        public static implicit operator LocalisedString(string text) => new LocalisedString((text, text), false);

        public override string ToString() => Text.Original;

        public bool Equals(LocalisedString other) =>
            Text == other.Text &&
            ShouldLocalise == other.ShouldLocalise &&
            Args.SequenceEqual(other.Args);
    }
}
