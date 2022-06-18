// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Graphics.Pooling
{
    public interface IDrawablePool
    {
        /// <summary>
        /// Get a drawable from this pool.
        /// </summary>
        /// <param name="setupAction">An optional action to be performed on this drawable immediately after retrieval. Should generally be used to prepare the drawable into a usable state.</param>
        /// <returns>The drawable.</returns>
        PoolableDrawable Get(Action<PoolableDrawable> setupAction = null);

        /// <summary>
        /// Return a drawable after use.
        /// </summary>
        /// <param name="pooledDrawable">The drwable to return. Should have originally come from this pool.</param>
        void Return(PoolableDrawable pooledDrawable);
    }
}
