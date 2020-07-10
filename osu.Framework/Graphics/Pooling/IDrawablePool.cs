// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Pooling
{
    public interface IDrawablePool
    {
        /// <summary>
        /// Return a drawable after use.
        /// </summary>
        /// <param name="pooledDrawable">The drwable to return. Should have originally come from this pool.</param>
        void Return(PoolableDrawable pooledDrawable);
    }
}
