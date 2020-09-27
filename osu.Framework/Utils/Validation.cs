// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using osuTK;
using osu.Framework.Graphics;

namespace osu.Framework.Utils
{
    public static class Validation
    {
        /// <summary>
        /// Returns whether the two coordinates of a <see cref="Vector2"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="float.IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="Vector2"/> to check.</param>
        /// <returns>False if X or Y are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(Vector2 toCheck) => float.IsFinite(toCheck.X) && float.IsFinite(toCheck.Y);

        /// <summary>
        /// Returns whether the components of a <see cref="MarginPadding"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="float.IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="MarginPadding"/> to check.</param>
        /// <returns>False if either component of <paramref name="toCheck"/> are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(MarginPadding toCheck) => float.IsFinite(toCheck.Top) && float.IsFinite(toCheck.Bottom) && float.IsFinite(toCheck.Left) && float.IsFinite(toCheck.Right);

        /// <summary>
        /// Returns whether the provided generic type <typeparamref name="T"/> is a numeric type (except for <see cref="decimal"/>, <c>false</c> is returned for it).
        /// </summary>
        /// <typeparam name="T">The generic type to check with</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumericType<T>() =>
            typeof(T) == typeof(sbyte)
            || typeof(T) == typeof(byte)
            || typeof(T) == typeof(short)
            || typeof(T) == typeof(ushort)
            || typeof(T) == typeof(int)
            || typeof(T) == typeof(uint)
            || typeof(T) == typeof(long)
            || typeof(T) == typeof(ulong)
            || typeof(T) == typeof(float)
            || typeof(T) == typeof(double);
    }
}
