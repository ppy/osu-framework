// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A pool for lists whose return to the pool can deferred with a `using` syntax.
    /// <code>
    /// using var list = pool.Rent();
    /// </code>
    /// </summary>
    public class ListPool<T>
    {
        private readonly ObjectPool<List<T>> pool = ObjectPool.Create<List<T>>();

        /// <summary>
        /// Gets a shared <see cref="ListPool{T}"/> instance.
        /// </summary>
        public static ListPool<T> Shared { get; } = new ListPool<T>();

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool. The returned list is empty.
        /// </summary>
        public List<T> Rent()
            => pool.Get();

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool. The returned list contains elements from the provided <paramref name="source"/>.
        /// </summary>
        public List<T> Rent(IEnumerable<T> source)
        {
            var list = pool.Get();
            list.AddRange(source);
            return list;
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> to the <see cref="ListPool{T}"/> from which it was previously rented.
        /// </summary>
        public void Return(List<T> list)
        {
            list.Clear();
            pool.Return(list);
        }
    }
}
