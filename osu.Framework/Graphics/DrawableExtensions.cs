// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Development;

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

        /// <summary>
        /// Find the closest parent of a specified type.
        /// </summary>
        /// <remarks>
        /// This can be a potentially expensive operation and should be used with discretion.
        /// </remarks>
        /// <typeparam name="T">The type to match.</typeparam>
        /// <returns>The first matching parent, or null if no parent of type <typeparamref name="T"/> is found.</returns>
        public static T FindClosestParent<T>(this Drawable drawable) where T : class, IDrawable
        {
            while ((drawable = drawable.Parent) != null)
            {
                if (drawable is T match)
                    return match;
            }

            return default;
        }

        /// <summary>
        /// Forces removal of this drawable from its parent, followed by immediate synchronous disposal.
        /// </summary>
        /// <remarks>
        /// This is intended as a temporary solution for the fact that there is no way to easily dispose
        /// a component in a way that is guaranteed to be synchronously run on the update thread.
        ///
        /// Eventually components will have a better method for unloading.
        /// </remarks>
        /// <param name="drawable">The <see cref="Drawable"/> to be disposed.</param>
        public static void RemoveAndDisposeImmediately(this Drawable drawable)
        {
            ThreadSafety.EnsureUpdateThread();

            drawable.Parent?.RemoveInternal(drawable);
            drawable.Dispose();
        }
    }
}
