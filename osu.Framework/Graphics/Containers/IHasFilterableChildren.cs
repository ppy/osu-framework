// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    [Obsolete("IFilterable children are now looked up automatically. Implementors of this interface should implement IFilterable directly instead.")] // can be removed 20230512
    public interface IHasFilterableChildren : IFilterable
    {
        /// <summary>
        /// List of children that can be filtered
        /// </summary>
        IEnumerable<IFilterable> FilterableChildren { get; }
    }
}
