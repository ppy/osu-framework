// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

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
        IEnumerable<string> FilterTerms { get; }
    }
}
