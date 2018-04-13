// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;

namespace osu.Framework.MathUtils
{
    public static class Validation
    {
        /// <summary>
        /// Returns the exponent of a (single-precision) <see cref="float"/> as byte.
        /// </summary>
        /// <param name="value">The <see cref="float"/> to get the exponent from.</param>
        /// <remarks>Returns a <see cref="byte"/> so it's a smaller data type (and faster to pass around).</remarks>
        /// <returns>The exponent (bit 2 to 8) of the single-point <see cref="float"/>.</returns>
        private static unsafe byte singleToExponentAsByte(float value) => (byte)(*(int*)&value >> 23);

        /// <summary>
        /// Returns whether a value is not <see cref="float.NegativeInfinity"/>, <see cref="float.PositiveInfinity"/> or <see cref="float.NaN"/>.
        /// </summary>
        /// <param name="toCheck"></param>
        /// <remarks>Is equivalent to (<see cref="float.IsNaN(float)"/> || <see cref="float.IsInfinity(float)"/>), but with less overhead.</remarks>
        /// <returns>Whether the float is valid in our conditions.</returns>
        public static bool IsFinite(float toCheck) => singleToExponentAsByte(toCheck) != byte.MaxValue;

        /// <summary>
        /// Returns whether the two coordinates of a <see cref="Vector2"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="Vector2"/> to check.</param>
        /// <returns>False if X or Y are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(Vector2 toCheck) => IsFinite(toCheck.X) && IsFinite(toCheck.Y);

        /// <summary>
        /// Returns whether the components of a <see cref="MarginPadding"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="MarginPadding"/> to check.</param>
        /// <returns>False if either component of <paramref name="toCheck"/> are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(MarginPadding toCheck) => IsFinite(toCheck.Top) && IsFinite(toCheck.Bottom) && IsFinite(toCheck.Left) && IsFinite(toCheck.Right);
    }
}
