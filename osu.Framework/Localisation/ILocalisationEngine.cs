// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Provides functionality to localise text and dynamically update localisations.
    /// </summary>
    public interface ILocalisationEngine
    {
        /// <summary>
        /// Create a localised <see cref="IBindable{T}"/> for a <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="localisable">
        /// The <see cref="LocalisableString"/> to get a bindable for.<para/>
        /// Changing any of its bindables' values will also trigger a localisation update, unless <see cref="LocalisableString.Type"/> is set to <see cref="LocalisationType.Never"/>.
        /// </param>
        IBindable<string> GetLocalisedBindable(LocalisableString localisable);

        /// <summary>
        /// Create a <see cref="IBindable{T}"/> that is one of the two provided unicode or non-unicode strings.
        /// </summary>
        /// <param name="unicode">The unicode version of the text.</param>
        /// <param name="nonUnicode">The non-unicode version of the text.</param>
        /// <returns>A <see cref="IBindable{T}"/> that is either <paramref name="unicode"/> or <paramref name="nonUnicode"/>, based on a setting.</returns>
        IBindable<string> GetUnicodeBindable(string unicode, string nonUnicode);
    }
}
