// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Utils
{
    public static class MathUtils
    {
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
