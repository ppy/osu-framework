// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        [Obsolete("Use float.DegressToRadians.")] // can be removed 20240901
        public static float DegreesToRadians(float degrees) => float.DegreesToRadians(degrees);

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        [Obsolete("Use double.DegressToRadians.")] // can be removed 20240901
        public static double DegreesToRadians(double degrees) => double.DegreesToRadians(degrees);

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        [Obsolete("Use float.RadiansToDegrees.")] // can be removed 20240901
        public static float RadiansToDegrees(float radians) => float.RadiansToDegrees(radians);

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        [Obsolete("Use double.RadiansToDegrees.")] // can be removed 20240901
        public static double RadiansToDegrees(double radians) => double.RadiansToDegrees(radians);

        /// <summary>
        /// Integer division with rounding up instead of down.
        /// </summary>
        /// <param name="value">The value to be divided.</param>
        /// <param name="divisor">The divisor of the division.</param>
        /// <returns>The rounded-up quotient.</returns>
        public static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }
    }
}
