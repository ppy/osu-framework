// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An interface to expose a number of keywords with the intent of helping a parent filter results.
    /// See <see cref="IFilterable"/> for an interface which adds a callback on matching keywords.
    /// </summary>
    public interface IHasFilterTerms
    {
        /// <summary>
        /// An enumerator of relevant terms which match the current object in a filtered scenario.
        /// </summary>
        IEnumerable<LocalisableString> FilterTerms { get; }
    }
}
