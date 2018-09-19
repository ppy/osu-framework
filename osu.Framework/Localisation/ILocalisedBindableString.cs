// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// An <see cref="IBindable{T}"/> which has its value set depending on the current locale of the <see cref="LocalisationManager"/>.
    /// </summary>
    public interface ILocalisedBindableString : IBindable<string>
    {
        /// <summary>
        /// Sets the original, un-localised text.
        /// </summary>
        LocalisableString Original { set; }
    }
}
