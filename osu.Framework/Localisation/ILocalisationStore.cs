// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Globalization;
using osu.Framework.IO.Stores;

namespace osu.Framework.Localisation
{
    public interface ILocalisationStore : IResourceStore<string>
    {
        /// <summary>
        /// The <see cref="CultureInfo"/> corresponding to the content of this <see cref="ILocalisationStore"/>
        /// and can be used for formatting numbers etc.
        /// </summary>
        CultureInfo EffectiveCulture { get; }
    }
}
