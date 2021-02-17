// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

#nullable enable

namespace osu.Framework.Extensions.ObjectExtensions
{
    /// <summary>
    /// Extensions that apply to all objects.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Coerces a nullable object as non-nullable. This is an alternative to the C# 8 null-forgiving operator "<c>!</c>".
        /// </summary>
        /// <remarks>
        /// This should only be used when an assertion or other handling is not a reasonable alternative.
        /// </remarks>
        /// <param name="obj">The nullable object.</param>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The non-nullable object corresponding to <paramref name="obj"/>.</returns>
        public static T AsNonNull<T>(this T? obj)
            where T : class
        {
            Debug.Assert(obj != null);
            return obj;
        }
    }
}
