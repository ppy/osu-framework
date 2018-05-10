// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class containing all necessary information to apply any (or any amount) of these changes to a string: Unicode preference, Localisation, Formatting (in this order).
    /// <para>Instead of setting <see cref="Type"/> to <see cref="LocalisationType.Localised"/>, you can just use a string instead.</para>
    /// </summary>
    public class LocalisableString
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting.
        /// </summary>
        public Bindable<string> Text { get; }

        /// <summary>
        /// What types of changes to apply to this string.
        /// </summary>
        public Bindable<LocalisationType> Type { get; }

        /// <summary>
        /// The arguments to be used in case this string is formattable.
        /// <para>If there are not enough arguments supplied here (which should never be the case), the text will not be formatted at all.</para>
        /// </summary>
        public Bindable<object[]> Args { get; }

        /// <summary>
        /// Create a <see cref="LocalisableString"/> instance containing all information needed to set a Unicode preference, localise, or format a string.
        /// </summary>
        /// <param name="text">The text to be used for localisation and/or formatting. See also <seealso cref="Text"/>.</param>
        /// <param name="type">What types of changes to apply. See also <seealso cref="Type"/>.<para>If you plan on setting this to <see cref="LocalisationType.None"/>, use the implicit string conversion instead.</para></param>
        /// <param name="args">The arguments to be used in case this string is formattable. See also <seealso cref="Args"/>.</param>
        public LocalisableString([NotNull] string text, LocalisationType type = LocalisationType.Localised, params object[] args)
        {
            Text = new Bindable<string>(text);
            Type = new Bindable<LocalisationType>(type);
            Args = new Bindable<object[]>(args);
        }

        // This is localised by default for convenience
        // A way to directly set unlocalised text as a string should be provided by the implementing class
        public static implicit operator LocalisableString(string localised) => new LocalisableString(localised);
    }
}
