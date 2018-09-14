// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class representing text that can be localised and formatted.
    /// </summary>
    public struct LocalisableString : IEquatable<LocalisableString>
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting.
        /// </summary>
        public Bindable<string> Text { get; }

        /// <summary>
        /// Whether <see cref="Text"/> should be localised.
        /// </summary>
        public Bindable<bool> Localised { get; }

        /// <summary>
        /// The arguments to format <see cref="Text"/> with.
        /// </summary>
        public Bindable<object[]> Args { get; }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="localised">Whether the text should be localised.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisableString(string text, bool localised = true, params object[] args)
        {
            Text = new Bindable<string>(text ?? string.Empty);
            Localised = new Bindable<bool>(localised);
            Args = new Bindable<object[]>(args);
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
        {
        }
    }
}
