// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Holds extension methods for <see cref="Drawable"/>.
    /// </summary>
    public static class DrawableExtensions
    {
        /// <summary>
        /// Adjusts specified properties of a <see cref="Drawable"/>.
        /// </summary>
        /// <param name="drawable">The <see cref="Drawable"/> whose properties should be adjusted.</param>
        /// <param name="adjustment">The adjustment function.</param>
        /// <returns>The given <see cref="Drawable"/>.</returns>
        public static T With<T>(this T drawable, Action<T> adjustment)
            where T : Drawable
        {
            adjustment?.Invoke(drawable);
            return drawable;
        }
    }
}
