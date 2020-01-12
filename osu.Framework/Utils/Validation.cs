// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Graphics;
using System;

namespace osu.Framework.Utils
{
    public static class Validation
    {
        /// <summary>
        /// Returns whether a value is not <see cref="float.NegativeInfinity"/>, <see cref="float.PositiveInfinity"/> or <see cref="float.NaN"/>.
        /// </summary>
        /// <param name="toCheck"></param>
        /// <returns>Whether the float is valid in our conditions.</returns>
        [Obsolete("Use float.IsFinite in .NET Standard 2.1")] // can be removed 20200626
        public static bool IsFinite(float toCheck) => float.IsFinite(toCheck);

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
    }
}
