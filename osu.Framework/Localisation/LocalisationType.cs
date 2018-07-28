// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using System;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Specifies the modifications the <see cref="ILocalisationEngine"/> will carry out on the supplied string.
    /// </summary>
    [Flags]
    public enum LocalisationType
    {
        /// <summary>
        /// The string will not be changed by localisation, and any changes to the bindable will not be tracked at all by the <see cref="ILocalisationEngine"/>.
        /// </summary>
        Never = 0,

        /// <summary>
        /// The string will not be changed by localisation.
        /// </summary>
        None = 1,

        /// <summary>
        /// The string will be localised based on the supplied embedded localisation files and the currently active <see cref="FrameworkSetting.Locale"/>.
        /// </summary>
        Localised = 1 << 1,

        /// <summary>
        /// The string will be formatted based on an object's value which will be re-evaluated on every <see cref="FrameworkSetting.Locale"/> change.
        /// <para>This is useful for e.g. date and time strings which differ from country to country.</para>
        /// </summary>
        Formatted = 1 << 2,
    }
}
