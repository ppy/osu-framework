// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Pooling
{
    /// <summary>
    /// A component which provides a pool of reusable drawables.
    /// Should be used to reduce allocation and construction overhead of individual drawables.
    /// </summary>
    /// <remarks>
    /// The <see cref="initialSize"/> drawables will be prepared ahead-of-time during this pool's asynchronous load procedure.
    /// Drawables exceeding the pool's available size will not be asynchronously loaded as it is assumed they are immediately required for consumption.
    /// </remarks>
    /// <typeparam name="T">The type of drawable to be pooled.</typeparam>
    public class DrawablePool<T> : CompositeDrawable, IDrawablePool where T : PoolableDrawable, new()
    {
        private static readonly GlobalStatistic<DrawablePoolUsageStatistic> usage_statistic = GlobalStatistics.Get<DrawablePoolUsageStatistic>(nameof(DrawablePool<T>), typeof(T).ReadableName());

        private readonly int initialSize;
        private readonly int? maximumSize;

        /// <summary>
        /// The number of drawables currently available for consumption.
        /// </summary>
        public int CountAvailable => pool.Count;

        private readonly Stack<T> pool = new Stack<T>();
        private GlobalStatistic<DrawablePoolUsageStatistic> statistic;

        static DrawablePool()
        {
            usage_statistic.Value = new DrawablePoolUsageStatistic();
        }

        /// <summary>
        /// Create a new pool instance.
        /// </summary>
        /// <param name="initialSize">The number of drawables to be prepared for initial consumption.</param>
        /// <param name="maximumSize">An optional maximum size after which the pool will no longer be expanded.</param>
        public DrawablePool(int initialSize, int? maximumSize = null)
        {
            this.maximumSize = maximumSize;
            this.initialSize = initialSize;

            statistic = usage_statistic;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            for (int i = 0; i < initialSize; i++)
                push(create());

            LoadComponents(pool.ToArray());
        }

        /// <summary>
        /// Return a drawable after use.
        /// </summary>
        /// <param name="pooledDrawable">The drawable to return. Should have originally come from this pool.</param>
        public void Return(PoolableDrawable pooledDrawable)
        {
            if (!(pooledDrawable is T))
                throw new ArgumentException("Invalid type", nameof(pooledDrawable));

            if (pooledDrawable.Parent != null)
                throw new InvalidOperationException("Drawable was attempted to be returned to pool while still in a hierarchy");

            if (pooledDrawable.IsInUse)
            {
                // if the return operation didn't come from the drawable, redirect to ensure consistent behaviour.
                pooledDrawable.Return();
                return;
            }

            //TODO: check the drawable was sourced from this pool for safety.
            push((T)pooledDrawable);
            countInUse--;
        }

        /// <summary>
        /// Get a drawable from this pool.
        /// </summary>
        /// <param name="setupAction">An optional action to be performed on this drawable immediately after retrieval. Should generally be used to prepare the drawable into a usable state.</param>
        /// <returns>The drawable.</returns>
        public T Get(Action<T> setupAction = null)
        {
            if (!pool.TryPop(out var drawable))
            {
                drawable = create();

                if (LoadState >= LoadState.Loading)
                    LoadComponent(drawable);
            }
            else
                countAvailable--;

            setupAction?.Invoke(drawable);
            drawable.Assign();
            countInUse++;

            return drawable;
        }

        /// <summary>
        /// Create a new drawable to be consumed or added to the pool.
        /// </summary>
        protected virtual T CreateNewDrawable() => new T();

        private T create()
        {
            var drawable = CreateNewDrawable();
            drawable.SetPool(this);
            countNewed++;

            return drawable;
        }

        private bool push(T poolableDrawable)
        {
            if (CountAvailable >= maximumSize)
            {
                // if the drawable can't be returned to the pool, mark it as such so it can be disposed of.
                poolableDrawable.SetPool(null);

                // then attempt disposal.
                if (poolableDrawable.DisposeOnDeathRemoval)
                    DisposeChildAsync(poolableDrawable);

                return false;
            }

            pool.Push(poolableDrawable);
            countAvailable++;

            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            countInUse = 0;
            countNewed = 0;
            countAvailable = 0;

            // Disallow any further Gets/Returns to adjust the statistics.
            statistic = null;
        }

        private int localCountInUse;

        private int countInUse
        {
            get => localCountInUse;
            set
            {
                if (statistic != null)
                    statistic.Value.CountInUse += value - localCountInUse;
                localCountInUse = value;
            }
        }

        private int localCountNewed;

        private int countNewed
        {
            get => localCountNewed;
            set
            {
                if (statistic != null)
                    statistic.Value.CountNewed += value - localCountNewed;
                localCountNewed = value;
            }
        }

        private int localCountAvailable;

        private int countAvailable
        {
            get => localCountAvailable;
            set
            {
                if (statistic != null)
                    statistic.Value.CountAvailable += value - localCountAvailable;
                localCountAvailable = value;
            }
        }

        private class DrawablePoolUsageStatistic
        {
            /// <summary>
            /// Total number of drawables available for use (in the pool).
            /// </summary>
            public int CountAvailable;

            /// <summary>
            /// Total number of drawables currently in use.
            /// </summary>
            public int CountInUse;

            /// <summary>
            /// Total number of drawables newed (can exceed max count).
            /// </summary>
            public int CountNewed;

            public override string ToString() => $"{CountAvailable}/{CountNewed} ({CountInUse})";
        }
    }
}
