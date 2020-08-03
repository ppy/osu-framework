// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Utility class to compare <see cref="float"/> or <see cref="double"/> values for equality.
    /// </summary>
    public static class Precision
    {
        /// <summary>
        /// The default epsilon for all <see cref="float"/> values.
        /// </summary>
        public const float FLOAT_EPSILON = 1e-3f;

        /// <summary>
        /// The default epsilon for all <see cref="double"/> values.
        /// </summary>
        public const double DOUBLE_EPSILON = 1e-7;

        /// <summary>
        /// Computes whether a value is definitely greater than another given an acceptable difference.
        /// </summary>
        /// <param name="value1">The value to compare.</param>
        /// <param name="value2">The value to compare against.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> is definitely greater than <paramref name="value2"/>.</returns>
        public static bool DefinitelyBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => value1 - acceptableDifference > value2;

        /// <summary>
        /// Computes whether a value is definitely greater than another given an acceptable difference.
        /// </summary>
        /// <param name="value1">The value to compare.</param>
        /// <param name="value2">The value to compare against.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> is definitely greater than <paramref name="value2"/>.</returns>
        public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => value1 - acceptableDifference > value2;

        /// <summary>
        /// Computes whether a value is almost greater than another given an acceptable difference.
        /// </summary>
        /// <param name="value1">The value to compare.</param>
        /// <param name="value2">The value to compare against.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> is almost greater than <paramref name="value2"/>.</returns>
        public static bool AlmostBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => value1 > value2 - acceptableDifference;

        /// <summary>
        /// Computes whether a value is almost greater than another given an acceptable difference.
        /// </summary>
        /// <param name="value1">The value to compare.</param>
        /// <param name="value2">The value to compare against.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> is almost greater than <paramref name="value2"/>.</returns>
        public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => value1 > value2 - acceptableDifference;

        /// <summary>
        /// Computes whether two values are equal within an acceptable difference.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
        public static bool AlmostEquals(float value1, float value2, float acceptableDifference = FLOAT_EPSILON) => Math.Abs(value1 - value2) <= acceptableDifference;

        /// <summary>
        /// Computes whether two <see cref="Vector2"/>s are equal within an acceptable difference.
        /// </summary>
        /// <param name="value1">The first <see cref="Vector2"/>.</param>
        /// <param name="value2">The second <see cref="Vector2"/>.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
        public static bool AlmostEquals(Vector2 value1, Vector2 value2, float acceptableDifference = FLOAT_EPSILON) => AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);

        /// <summary>
        /// Computes whether two values are equal within an acceptable difference.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="DOUBLE_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="value1"/> and <paramref name="value2"/> are almost equal.</returns>
        public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON) => Math.Abs(value1 - value2) <= acceptableDifference;

        /// <summary>
        /// Computes whether two <see cref="RectangleF"/>s intersect within an acceptable difference.
        /// </summary>
        /// <param name="rect1">The first <see cref="RectangleF"/>.</param>
        /// <param name="rect2">The second <see cref="RectangleF"/>.</param>
        /// <param name="acceptableDifference">The acceptable difference. Defaults to <see cref="FLOAT_EPSILON"/>.</param>
        /// <returns>Whether <paramref name="rect1"/> and <paramref name="rect2"/> intersect.</returns>
        public static bool AlmostIntersects(RectangleF rect1, RectangleF rect2, float acceptableDifference = FLOAT_EPSILON)
            => rect1.X <= rect2.X + rect2.Width + acceptableDifference
               && rect2.X <= rect1.X + rect1.Width + acceptableDifference
               && rect1.Y <= rect2.Y + rect2.Height + acceptableDifference
               && rect2.Y <= rect1.Y + rect1.Height + acceptableDifference;
    }
}
