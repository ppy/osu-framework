// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Logging;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// A store that can be used to track application-wide statistics and monitor runtime components.
    /// </summary>
    public static class GlobalStatistics
    {
        /// <summary>
        /// An event which is raised when the available statistics change.
        /// </summary>
        internal static event NotifyCollectionChangedEventHandler StatisticsChanged;

        private static readonly BindableList<IGlobalStatistic> statistics = new BindableList<IGlobalStatistic>();

        static GlobalStatistics()
        {
            statistics.BindCollectionChanged((o, e) => StatisticsChanged?.Invoke(o, e));
        }

        /// <summary>
        /// Retrieve a <see cref="IGlobalStatistic"/> of specified type.
        /// If no matching statistic already exists, a new instance is created and registered automatically.
        /// </summary>
        /// <param name="group">The group specification.</param>
        /// <param name="name">The name specification.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static GlobalStatistic<T> Get<T>(string group, string name)
        {
            lock (statistics)
            {
                var existing = statistics.OfType<GlobalStatistic<T>>().FirstOrDefault(s => s.Name == name && s.Group == group);
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
                for (int i = 0; i < statistics.Count; i++)
                {
                    if (group?.Equals(statistics[i].Group, StringComparison.Ordinal) != false)
                        statistics.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        /// Remove a specific statistic.
        /// </summary>
        /// <param name="statistic">The statistic to remove.</param>
        public static void Remove(IGlobalStatistic statistic)
        {
            lock (statistics)
                statistics.Remove(statistic);
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

        public static void OutputToLog()
        {
            var statisticsSnapshot = GetStatistics();

            Logger.Log("----- Global Statistics -----", LoggingTarget.Performance);

            foreach (var group in statisticsSnapshot.GroupBy(s => s.Group))
            {
                Logger.Log($"# {group.Key}", LoggingTarget.Performance);

                foreach (var i in group)
                    Logger.Log($"{i.Name,-30}: {i.DisplayValue}", LoggingTarget.Performance);
            }

            Logger.Log("--- Global Statistics End ---", LoggingTarget.Performance);
        }

        public static IGlobalStatistic[] GetStatistics()
        {
            lock (statistics)
                return statistics.ToArray();
        }
    }
}
