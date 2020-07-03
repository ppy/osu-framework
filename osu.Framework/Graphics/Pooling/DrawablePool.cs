// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Pooling
{
    /// <summary>
    /// A component which provides a pool of reusable drawables.
    /// Should be used to reduce allocation and construction overhead of individual drawables.
    /// </summary>
    /// <typeparam name="T">The type of drawable to be pooled.</typeparam>
    public class DrawablePool<T> : Component, IDrawablePool where T : PoolableDrawable, new()
    {
        private readonly int initialSize;
        private readonly int? maximumSize;

        /// <summary>
        /// The number of drawables currently available for consumption.
        /// </summary>
        public int CountAvailable => pool.Count;

        private readonly Stack<T> pool = new Stack<T>();

        /// <summary>
        /// create a new pool instance.
        /// </summary>
        /// <param name="initialSize">The number of drawables to be prepared for initial consumption.</param>
        /// <param name="maximumSize">An optional maximum size after which the pool will no longer be expanded.</param>
        public DrawablePool(int initialSize, int? maximumSize = null)
        {
            this.maximumSize = maximumSize;
            this.initialSize = initialSize;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            for (int i = 0; i < initialSize; i++)
                push(create());
        }

        /// <summary>
        /// Return a drawable after use.
        /// </summary>
        /// <param name="pooledDrawable">The drawable to return. Should have originally come from this pool.</param>
        public void Return(Drawable pooledDrawable)
        {
            if (!(pooledDrawable is T))
                throw new ArgumentException("Invalid type", nameof(pooledDrawable));

            //TODO: check the drawable was sourced from this pool for safety.
            push((T)pooledDrawable);
        }

        /// <summary>
        /// Get a drawable
        /// </summary>
        /// <param name="setupAction"></param>
        /// <returns></returns>
        public T Get(Action<T> setupAction = null)
        {
            if (!pool.TryPop(out var drawable))
                drawable = create();

            setupAction?.Invoke(drawable);
            drawable.Assign();

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

        private bool push(T poolableDrawable)
        {
            if (CountAvailable >= maximumSize)
            {
                // if the drawable can't be returned to the pool, mark it as such so it can be disposed of.
                poolableDrawable.SetPool(null);
                return false;
            }

            pool.Push(poolableDrawable);
            return true;
        }
    }
}
