// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Lists;

namespace osu.Framework.Logging
{
    internal static class LoadingComponentsLogger
    {
        private static readonly WeakList<Drawable> loading_components = new WeakList<Drawable>();

        public static void Add(Drawable component)
        {
            if (!DebugUtils.IsDebugBuild) return;

            lock (loading_components)
                loading_components.Add(component);
        }

        public static void Remove(Drawable component)
        {
            if (!DebugUtils.IsDebugBuild) return;

            lock (loading_components)
                loading_components.Remove(component);
        }

        public static void LogAndFlush()
        {
            if (!DebugUtils.IsDebugBuild) return;

            lock (loading_components)
            {
                Logger.Log($"⏳ Currently loading components ({loading_components.Count()})");

                foreach (var c in loading_components.OrderBy(c => c.LoadThread?.Name).ThenBy(c => c.LoadState))
                {
                    Logger.Log(c.ToString());
                    Logger.Log($"- thread: {c.LoadThread?.Name ?? "none"}");
                    Logger.Log($"- state:  {c.LoadState}");
                }

                loading_components.Clear();

                Logger.Log("🧵 Task schedulers");

                Logger.Log(CompositeDrawable.SCHEDULER_STANDARD.GetStatusString());
                Logger.Log(CompositeDrawable.SCHEDULER_LONG_LOAD.GetStatusString());
            }

            ThreadPool.GetAvailableThreads(out int workerAvailable, out int completionAvailable);
            ThreadPool.GetMinThreads(out int workerMin, out int completionMin);
            ThreadPool.GetMaxThreads(out int workerMax, out int completionMax);

            Logger.Log("🎱 Thread pool");
            // TODO: use after net6
            // Logger.Log($"threads:         {ThreadPool.ThreadCount:#,0}");
            // Logger.Log($"work pending:    {ThreadPool.PendingWorkItemCount:#,0}");
            // Logger.Log($"work completed:  {ThreadPool.CompletedWorkItemCount:#,0}");
            Logger.Log($"worker:          min {workerMin,-6:#,0} max {workerMax,-6:#,0} available {workerAvailable,-6:#,0}");
            Logger.Log($"completion:      min {completionMin,-6:#,0} max {completionMax,-6:#,0} available {completionAvailable,-6:#,0}");
        }
    }
}
