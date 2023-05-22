// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
    public partial class DrawablePool<T> : CompositeDrawable, IDrawablePool where T : PoolableDrawable, new()
    {
        private GlobalStatistic<DrawablePoolUsageStatistic> statistic;

        private readonly int initialSize;
        private readonly int? maximumSize;

        private readonly Stack<T> pool = new Stack<T>();

        // ReSharper disable once StaticMemberInGenericType (this is intentional, we want a separate count per type).
        private static int poolInstanceID;

        /// <summary>
        /// Create a new pool instance.
        /// </summary>
        /// <param name="initialSize">The number of drawables to be prepared for initial consumption.</param>
        /// <param name="maximumSize">An optional maximum size after which the pool will no longer be expanded.</param>
        public DrawablePool(int initialSize, int? maximumSize = null)
        {
            if (initialSize > maximumSize)
                throw new ArgumentOutOfRangeException(nameof(initialSize), "Initial size must be less than or equal to maximum size.");

            this.maximumSize = maximumSize;
            this.initialSize = initialSize;

            int id = Interlocked.Increment(ref poolInstanceID);

            statistic = GlobalStatistics.Get<DrawablePoolUsageStatistic>(nameof(DrawablePool<T>), typeof(T).ReadableName() + $"`{id}");
            statistic.Value = new DrawablePoolUsageStatistic();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            for (int i = 0; i < initialSize; i++)
                pool.Push(create());
            CurrentPoolSize = initialSize;

            LoadComponents(pool.ToArray());
        }

        /// <summary>
        /// Return a drawable after use.
        /// </summary>
        /// <param name="pooledDrawable">The drawable to return. Should have originally come from this pool.</param>
        public void Return(PoolableDrawable pooledDrawable)
        {
            if (pooledDrawable is not T typedDrawable)
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

            if (CountAvailable >= maximumSize)
            {
                // if the drawable can't be returned to the pool, mark it as such so it can be disposed of.
                pooledDrawable.SetPool(null);

                // then attempt disposal.
                if (pooledDrawable.DisposeOnDeathRemoval)
                    DisposeChildAsync(pooledDrawable);
            }
            else
            {
                pool.Push(typedDrawable);
            }

            CountInUse--;
        }

        PoolableDrawable IDrawablePool.Get(Action<PoolableDrawable> setupAction) => Get(setupAction);

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

                if (maximumSize == null || currentPoolSize < maximumSize)
                {
                    CurrentPoolSize++;
                    Debug.Assert(maximumSize == null || CurrentPoolSize <= maximumSize);
                }
                else
                    CountExcessConstructed++;

                if (LoadState >= LoadState.Loading)
                    LoadComponent(drawable);
            }

            CountInUse++;

            drawable.Assign();
            drawable.LifetimeStart = double.MinValue;
            drawable.LifetimeEnd = double.MaxValue;

            setupAction?.Invoke(drawable);

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

            return drawable;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            foreach (var p in pool)
                p.Dispose();

            CountInUse = 0;
            CountExcessConstructed = 0;
            CurrentPoolSize = 0;

            GlobalStatistics.Remove(statistic);

            // Disallow any further Gets/Returns to adjust the statistics.
            statistic = null;
        }

        private int currentPoolSize;

        /// <summary>
        /// The current size of the pool.
        /// </summary>
        public int CurrentPoolSize
        {
            get => currentPoolSize;
            private set
            {
                Debug.Assert(statistic != null);

                statistic.Value.CurrentPoolSize = currentPoolSize = value;
            }
        }

        private int countInUse;

        /// <summary>
        /// The number of drawables currently in use.
        /// </summary>
        public int CountInUse
        {
            get => countInUse;
            private set
            {
                Debug.Assert(statistic != null);

                statistic.Value.CountInUse = countInUse = value;
            }
        }

        private int countExcessConstructed;

        /// <summary>
        /// The total number of drawables constructed that were not pooled.
        /// </summary>
        public int CountExcessConstructed
        {
            get => countExcessConstructed;
            private set
            {
                Debug.Assert(statistic != null);

                statistic.Value.CountExcessConstructed = countExcessConstructed = value;
            }
        }

        /// <summary>
        /// The number of drawables currently available for consumption.
        /// </summary>
        public int CountAvailable => pool.Count;

        private class DrawablePoolUsageStatistic
        {
            /// <summary>
            /// Total number of drawables available for use (in the pool).
            /// </summary>
            public int CurrentPoolSize;

            /// <summary>
            /// Total number of drawables currently in use.
            /// </summary>
            public int CountInUse;

            /// <summary>
            /// Total number of drawables constructed that were not pooled.
            /// </summary>
            public int CountExcessConstructed;

            public override string ToString() => $"{CountInUse}/{CurrentPoolSize} ({CountExcessConstructed} excess)";
        }
    }
}
