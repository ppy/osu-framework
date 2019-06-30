// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// A store that can be used to track application-wide statistics and monitor runtime components.
    /// </summary>
    public static class GlobalStatistics
    {
        internal static BindableList<IGlobalStatistic> Statistics { get; } = new BindableList<IGlobalStatistic>();

        public static void Register(IGlobalStatistic stat) => Statistics.Add(stat);
    }
}
