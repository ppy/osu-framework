// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
