// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Statistics
{
    public interface IGlobalStatistic
    {
        /// <summary>
        /// Statistic's visual grouping.
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Statistic's identifier.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Human readable value.
        /// </summary>
        string DisplayValue { get; }

        /// <summary>
        /// Clear the value of this statistic.
        /// </summary>
        void Clear();
    }
}
