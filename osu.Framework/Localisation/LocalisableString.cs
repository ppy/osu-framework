// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public class LocalisableString
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting. This will only be used if <see cref="FrameworkSetting.ShowUnicode"/> is set to true and/or <see cref="NonUnicode"/> is null.
        /// </summary>
        public Bindable<string> Text { get; }

        /// <summary>
        /// An alternative to <see cref="Text"/> that is used when <see cref="FrameworkSetting.ShowUnicode"/> is set to false and this value is non-null.
        /// <para>This means that if you want the displayed text to disappear when <see cref="FrameworkSetting.ShowUnicode"/> is false, you have to set this value to <see cref="string.Empty"/>.</para>
        /// </summary>
        public Bindable<string> NonUnicode { get; }

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
        /// <param name="nonUnicode">An alternative to '<paramref name="text"/>' that is used when Unicode text isn't allowed. See also <seealso cref="NonUnicode"/>.</param>
        /// <param name="args">The arguments to be used in case this string is formattable. See also <seealso cref="Args"/>.</param>
        public LocalisableString([CanBeNull] string text, LocalisationType type, string nonUnicode = null, params object[] args)
        {
            Text = new Bindable<string>(text);
            NonUnicode = new Bindable<string>(nonUnicode);
            Type = new Bindable<LocalisationType>(type);
            Args = new Bindable<object[]>(args);
        }

        public static implicit operator LocalisableString(string unlocalised) => new LocalisableString(unlocalised, LocalisationType.None);
    }
}
