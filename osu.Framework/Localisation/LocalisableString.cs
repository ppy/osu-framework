// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class containing all necessary information to apply any (or any amount) of these changes to a string: Unicode preference, Localisation, Formatting (in this order).
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
        /// Create a <see cref="LocalisableString"/> instance containing all information needed to set a Unicode preference, localise, or format a string.
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
