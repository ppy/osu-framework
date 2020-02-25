// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// A store that can be used to track application-wide statistics and monitor runtime components.
    /// </summary>
    public static class GlobalStatistics
    {
        // ReSharper disable once InconsistentlySynchronizedField
        internal static IBindableList<IGlobalStatistic> Statistics => statistics;

        private static readonly BindableList<IGlobalStatistic> statistics = new BindableList<IGlobalStatistic>();

        /// <summary>
        /// Retrieve a <see cref="IGlobalStatistic"/> of specified type.
        /// If no matching statistic already exists, a new instance is created and registered automatically.
        /// </summary>
        /// <param name="group">The group specification.</param>
        /// <param name="name">The name specification.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns></returns>
        public static GlobalStatistic<T> Get<T>(string group, string name)
        {
            lock (statistics)
            {
                var existing = Statistics.OfType<GlobalStatistic<T>>().FirstOrDefault(s => s.Name == name && s.Group == group);
                if (existing != null)
                    return existing;

                var newStat = new GlobalStatistic<T>(group, name);
                register(newStat);
                return newStat;
            }
        }

        /// <summary>
        /// Clear all statistics.
        /// </summary>
        /// <param name="group">An optional group identifier, limiting the clear operation to the matching group.</param>
        public static void Clear(string group = null)
        {
            lock (statistics)
            {
                foreach (var stat in statistics.Where(s => group?.Equals(s.Group, StringComparison.Ordinal) != false).ToArray())
                    statistics.Remove(stat);
            }
        }

        /// <summary>
        /// Register a new statistic type.
        /// </summary>
        /// <param name="stat">The statistic to register.</param>
        private static void register(IGlobalStatistic stat)
        {
            lock (statistics)
                statistics.Add(stat);
        }
    }
}
