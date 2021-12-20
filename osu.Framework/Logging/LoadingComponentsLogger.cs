// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
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
                    Logger.Log($"{c.GetType().ReadableName()}");
                    Logger.Log($"- thread: {c.LoadThread?.Name ?? "none"}");
                    Logger.Log($"- state:  {c.LoadState}");
                }

                loading_components.Clear();

                Logger.Log("🧵 Task schedulers");

                Logger.Log(CompositeDrawable.SCHEDULER_STANDARD.GetStatusString());
                Logger.Log(CompositeDrawable.SCHEDULER_LONG_LOAD.GetStatusString());
            }
        }
    }
}
