// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class containing representing a string that can be localised and formatted.
    /// </summary>
    public class LocalisableString
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting.
        /// </summary>
        public Bindable<string> Text { get; }

        /// <summary>
        /// Whether this string should be localised.
        /// </summary>
        public Bindable<bool> Localised { get; }

        /// <summary>
        /// The arguments to format the string with.
        /// </summary>
        public Bindable<object[]> Args { get; }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="text">The text to be used for localisation and/or formatting.</param>
        /// <param name="localised">Whether this string should be localised.</param>
        /// <param name="args">The arguments to format the string with.</param>
        public LocalisableString([NotNull] string text, bool localised = true, params object[] args)
        {
            Text = new Bindable<string>(text);
            Localised = new Bindable<bool>(localised);
            Args = new Bindable<object[]>(args);
        }
    }
}
