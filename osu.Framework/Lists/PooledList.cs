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
        private ObjectPool<PooledList> pool;

        public ListPool()
        {
            var policy = new Policy(this);
            pool = ObjectPool.Create(policy);
        }

        public static ListPool<T> Shared { get; } = new ListPool<T>();

        public PooledList Rent()
            => pool.Get();

        public void Return(PooledList list)
            => pool.Return(list);

        public class PooledList : List<T>, IDisposable
        {
            private ListPool<T> pool;

            public PooledList(ListPool<T> pool)
            {
                this.pool = pool;
            }

            [Obsolete("This is a workaround for the default ObjectPool implementation needing a T : new()")]
            public PooledList() { throw new InvalidOperationException($"{nameof(PooledList)} can not be instantiated outside of a pool."); }

            public void Dispose()
                => pool.Return(this);
        }

        private class Policy : IPooledObjectPolicy<PooledList>
        {
            private ListPool<T> pool;

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
