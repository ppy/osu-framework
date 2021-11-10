// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Utils
{
    /// <summary>
    /// An interface that defines the interpolation of a value.
    /// </summary>
    /// <typeparam name="TValue">The type of value to be interpolated..</typeparam>
    public interface IInterpolable<TValue>
    {
        /// <summary>
        /// Interpolates between two <typeparamref name="TValue"/>s.
        /// </summary>
        /// <remarks>
        /// This method MUST NOT modify the current object.
        /// </remarks>
        /// <param name="time">The current time.</param>
        /// <param name="startValue">The <typeparamref name="TValue"/> at <paramref name="time"/> = <paramref name="startTime"/>.</param>
        /// <param name="endValue">The <typeparamref name="TValue"/> at <paramref name="time"/> = <paramref name="endTime"/>.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="easing">The easing function to use.</param>
        /// <returns>The interpolated value.</returns>
        TValue ValueAt<TEasing>(double time, TValue startValue, TValue endValue, double startTime, double endTime, in TEasing easing) where TEasing : IEasingFunction;
    }
}
