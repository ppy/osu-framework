// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;

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
        LocalisableString Text { set; }
    }
}
