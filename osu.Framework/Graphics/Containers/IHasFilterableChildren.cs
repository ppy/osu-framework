// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface IHasFilterableChildren : IFilterable
    {
        /// <summary>
        /// List of children that can be filtered
        /// </summary>
        IEnumerable<IFilterable> FilterableChildren { get; }
    }
}
