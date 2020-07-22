// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Extensions.ListExtensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Creates a new read-only view into a <see cref="List{T}"/>.
        /// </summary>
        /// <remarks>Enumeration does not allocate the enumerator.</remarks>
        /// <param name="list">The list to create the view of.</param>
        /// <typeparam name="T">The type of elements contained by the list.</typeparam>
        /// <returns>The read-only view.</returns>
        public static SlimReadOnlyListWrapper<T> AsSlimReadOnly<T>(this List<T> list)
            => new SlimReadOnlyListWrapper<T>(list);
    }
}
