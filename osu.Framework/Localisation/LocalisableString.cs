// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisableString
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Whether <see cref="Text"/> should be localised.
        /// </summary>
        public readonly bool Localised;

        /// <summary>
        /// The arguments to format <see cref="Text"/> with.
        /// </summary>
        public readonly object[] Args;

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="localised">Whether the text should be localised.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisableString(string text, bool localised = true, params object[] args)
        {
            Text = text ?? string.Empty;
            Localised = localised;
            Args = args;
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>. This localises by default.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisableString(string text, params object[] args)
            : this(text, true, args)
        {
        }

        public static implicit operator string(LocalisableString localisable) => localisable.Text;

        public static implicit operator LocalisableString(string text) => new LocalisableString(text);
    }
}
