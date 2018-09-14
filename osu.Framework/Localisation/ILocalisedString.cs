// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A <see cref="IBindable{T}"/> which has its value set depending on the current localisation by the <see cref="LocalisationEngine"/>.
    /// </summary>
    public interface ILocalisedString : IBindable<string>
    {
        /// <summary>
        /// Sets the original, un-localised text.
        /// </summary>
        LocalisableString Original { set; }
    }
}
