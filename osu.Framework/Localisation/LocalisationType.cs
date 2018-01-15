// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public enum LocalisationType
    {
        /// <summary>
        /// The string will not be changed by localisation.
        /// </summary>
        None,

        /// <summary>
        /// The string will be one of two options depending on the currently active <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        UnicodePreference,

        /// <summary>
        /// The string will be localised based on the supplied embedded localisation files and the currently active <see cref="FrameworkSetting.Locale"/>.
        /// </summary>
        Localised,

        /// <summary>
        /// The string will be formatted based on an object's value which will be re-evaluated on every <see cref="FrameworkSetting.Locale"/> change.
        /// <para>This is useful for e.g. date and time strings which differ from country to country.</para>
        /// </summary>
        Formatted,

        /// <summary>
        /// The string will be localised and then formatted.
        /// </summary>
        FormattedLocalised,
    }
}
