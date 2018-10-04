// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// An <see cref="IBindable{T}"/> which has its value set based on the user's unicode preference.
    /// </summary>
    public interface IUnicodeBindableString : IBindable<string>
    {
        /// <summary>
        /// The text to use if unicode can be displayed. Can be null, in which case <see cref="NonUnicodeText"/> will be used.
        /// </summary>
        string UnicodeText { [CanBeNull] set; }

        /// <summary>
        /// The text to use if unicode should not be displayed. Can be null, in which case <see cref="UnicodeText"/> will be used.
        /// </summary>
        string NonUnicodeText { [CanBeNull] set; }
    }
}
