// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Bindables;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// A store that can be used to track application-wide statistics and monitor runtime components.
    /// </summary>
    public static class GlobalStatistics
    {
        internal static BindableList<IGlobalStatistic> Statistics { get; } = new BindableList<IGlobalStatistic>();

        private static readonly object statistics_lock = new object();

        /// <summary>
        /// Register a new statistic type.
        /// </summary>
        /// <param name="stat">The statistic to register.</param>
        public static void Register(IGlobalStatistic stat)
        {
            lock (statistics_lock)
                Statistics.Add(stat);
        }

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
            lock (statistics_lock)
            {
                var existing = Statistics.OfType<GlobalStatistic<T>>().FirstOrDefault(s => s.Name == name && s.Group == group);
                if (existing != null)
                    return existing;

                var newStat = new GlobalStatistic<T>(group, name);
                Register(newStat);
                return newStat;
            }
        }
    }
}
