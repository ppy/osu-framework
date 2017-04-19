// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface IFilterable
    {
        /// <summary>
        /// Array of keywords that it can be filtered with
        /// </summary>
        string[] Keywords { get; }
        /// <summary>
        /// Current filtered state, changes whenever it's getting filtered
        /// </summary>
        bool FilteredByParent { set; }
    }

    public interface IFilterableChildren : IFilterable
    {
        /// <summary>
        /// List of children that should be filtered
        /// </summary>
        IEnumerable<IFilterable> FilterableChildren { get; }
    }
}
