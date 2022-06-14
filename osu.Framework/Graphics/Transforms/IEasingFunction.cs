// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// An interface for an easing function that is applied to <see cref="Transform{TValue}"/>s.
    /// </summary>
    public interface IEasingFunction
    {
        /// <summary>
        /// Applies the easing function to a time value.
        /// </summary>
        /// <param name="time">The time value to apply the easing to.</param>
        /// <returns>The eased time value.</returns>
        double ApplyEasing(double time);
    }
}
