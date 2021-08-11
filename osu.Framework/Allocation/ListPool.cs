// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Provides a resource pool that enables reusing instances of type <see cref="PooledList"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the pooled lists.</typeparam>
    public class ListPool<T>
    {
        /// <summary>
        /// Gets a shared <see cref="ListPool{T}"/> instance.
        /// </summary>
        public static ListPool<T> Shared { get; } = new ListPool<T>();

        private readonly Stack<PooledList> available = new Stack<PooledList>();

        /// <summary>
        /// All <see cref="PooledList"/> which were created in this pool and are currently rented. Returning a <see cref="PooledList"/> to the pool that is not in this set will cause an exception.
        /// </summary>
        private readonly HashSet<PooledList> rented = new HashSet<PooledList>();

        /// <summary>
        /// Retrives the first avaiable <see cref="PooledList"/>.
        /// </summary>
        public PooledList Rent()
        {
            var list = available.Count == 0 ? new PooledList(this) : available.Pop();
            rented.Add(list);

            return list;
        }

        /// <summary>
        /// Returns a list that was previously rented via <see cref="Rent"/> of the same <see cref="ListPool{T}"/> instance to the pool.
        /// </summary>
        /// <param name="list">The list that was rented.</param>
        public void Return(PooledList list)
        {
            if (rented.Remove(list))
            {
                list.Clear();
                available.Push(list);
            }
            else
            {
                throw new InvalidOperationException($"Tried to return a {nameof(PooledList)} to a {nameof(ListPool<T>)} which it did not belong to or was already returned.");
            }
        }

        /// <summary>
        /// A list pooled from a <see cref="ListPool{T}"/>. The proper usage of this list is:
        /// <code>
        /// using var myList = myPool.Rent();
        /// </code>
        /// </summary>
        public class PooledList : List<T>, IDisposable
        {
            private readonly ListPool<T> pool;

            internal PooledList(ListPool<T> pool)
            {
                this.pool = pool;
            }

            public void Dispose()
            {
                pool.Return(this);
            }
        }
    }
}
