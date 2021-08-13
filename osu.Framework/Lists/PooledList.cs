// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        private readonly ObjectPool<PooledList> pool;

        public ListPool()
        {
            var policy = new Policy(this);
            pool = ObjectPool.Create(policy);
        }

        /// <summary>
        /// Gets a shared <see cref="ListPool{T}"/> instance.
        /// </summary>
        public static ListPool<T> Shared { get; } = new ListPool<T>();

        /// <summary>
        /// Retrieves a <see cref="PooledList"/> from the pool. The returned list is empty.
        /// </summary>
        public PooledList Rent()
            => pool.Get();

        /// <summary>
        /// Retrieves a <see cref="PooledList"/> from the pool. The returned list contains elements from the provided <paramref name="source"/>.
        /// </summary>
        public PooledList Rent(IEnumerable<T> source)
        {
            var list = pool.Get();
            list.AddRange(source);
            return list;
        }

        /// <summary>
        /// Returns a <see cref="PooledList"/> to the <see cref="ListPool{T}"/> from which it was previously rented.
        /// </summary>
        public void Return(PooledList list)
            => pool.Return(list);

        /// <summary>
        /// A <see cref="List{T}"/> pooled from a <see cref="ListPool{T}"/>. It can be returned to the pool by disposing it.
        /// <code>
        /// using var list = pool.Rent();
        /// </code>
        /// </summary>
        public class PooledList : List<T>, IDisposable
        {
            private readonly ListPool<T> pool;

            /// <summary>
            /// This should not be used by anyone but the <see cref="ListPool{T}"/> in which it is created.
            /// </summary>
            public PooledList(ListPool<T> pool)
            {
                this.pool = pool;
            }

            /// <summary>
            /// This is a workaround for the default ObjectPool implementation needing a T : new(). Do not use this.
            /// </summary>
            public PooledList() { throw new InvalidOperationException($"{nameof(PooledList)} can not be instantiated outside of a pool."); }

            public void Dispose()
                => pool.Return(this);
        }

        private class Policy : IPooledObjectPolicy<PooledList>
        {
            private readonly ListPool<T> pool;

            public Policy(ListPool<T> pool)
            {
                this.pool = pool;
            }

            public PooledList Create()
                => new PooledList(pool);

            public bool Return(PooledList obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
