// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Timing;

namespace osu.Framework.Statistics
{
    internal static class TypePerformanceMonitor
    {
        private static readonly StopwatchClock clock = new StopwatchClock(true);

        private static readonly Stack<Type> current_collection_type_stack = new Stack<Type>();

        private static readonly Dictionary<Type, double> collected_times = new Dictionary<Type, double>();

        private static double consumptionTime;

        private static bool active;

        public static bool Active;

        /// <summary>
        /// Start collecting time for a specific object type.
        /// </summary>
        internal static void BeginCollecting(object obj)
        {
            if (!active) return;

            var type = obj.GetType();

            if (current_collection_type_stack.Count > 0)
            {
                Type t = current_collection_type_stack.Peek();

                if (!collected_times.ContainsKey(t)) collected_times[t] = 0;
                collected_times[t] += consumeStopwatchElapsedTime();
            }

            current_collection_type_stack.Push(type);
        }

        /// <summary>
        /// End collecting time for a specific object type.
        /// </summary>
        internal static void EndCollecting(object obj)
        {
            if (!active) return;

            var type = obj.GetType();

            current_collection_type_stack.Pop();

            if (!collected_times.ContainsKey(type)) collected_times[type] = 0;
            collected_times[type] += consumeStopwatchElapsedTime();
        }

        private static double lastReport;

        private static int framesSinceLastReport;

        private const string group_name = "Update (Top CPU consumers)";

        public static void NewFrame()
        {
            if (Active != active)
            {
                active = Active;

                if (!active)
                {
                    lastReport = 0;
                    GlobalStatistics.Clear(group_name);
                    collected_times.Clear();
                    framesSinceLastReport = 0;
                    return;
                }
            }

            //reset frame totals
            current_collection_type_stack.Clear();
            consumeStopwatchElapsedTime();

            if (framesSinceLastReport > 0 && clock.CurrentTime - lastReport > 1000)
            {
                GlobalStatistics.Clear(group_name);

                int i = 0;
                foreach (var t in collected_times.OrderByDescending(t => t.Value).Take(5))
                    GlobalStatistics.Get<string>(group_name, $"{++i}. {t.Key.Name}").Value = $"{t.Value / framesSinceLastReport:N1}ms";

                collected_times.Clear();
                framesSinceLastReport = 0;
                lastReport = clock.CurrentTime;
            }

            framesSinceLastReport++;
        }

        private static double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;

            consumptionTime = clock.CurrentTime;

            return consumptionTime - last;
        }
    }
}
