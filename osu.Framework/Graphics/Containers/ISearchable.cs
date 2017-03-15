// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface ISearchable
    {
        /// <summary>
        /// List of keywords that <see cref="SearchContainer"/> should match
        /// </summary>
        string[] Keywords { get; }
        /// <summary>
        /// Current matching state
        /// </summary>
        bool Matching { set; }
    }

    public interface ISearchableChildren : ISearchable
    {
        /// <summary>
        /// List of <see cref="SearchContainer"/> should match
        /// </summary>
        IEnumerable<ISearchable> SearchableChildren { get; }
    }
}
