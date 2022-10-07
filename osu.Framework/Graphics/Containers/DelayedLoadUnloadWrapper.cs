// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Statistics;
using System.Diagnostics;
using osu.Framework.Layout;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Containers
{
    public class DelayedLoadUnloadWrapper : DelayedLoadWrapper
    {
        private readonly double timeBeforeUnload;

        public DelayedLoadUnloadWrapper(Func<Drawable> createContentFunction, double timeBeforeLoad = 500, double timeBeforeUnload = 1000)
            : base(createContentFunction, timeBeforeLoad)
        {
            this.timeBeforeUnload = timeBeforeUnload;

            AddLayout(unloadClockBacking);
        }

        private static readonly GlobalStatistic<int> total_loaded = GlobalStatistics.Get<int>("Drawable", $"{nameof(DelayedLoadUnloadWrapper)}s");

        private double timeHidden;

        private ScheduledDelegate unloadSchedule;

        protected bool ShouldUnloadContent => timeBeforeUnload == 0 || timeHidden > timeBeforeUnload;

        private ScheduledDelegate scheduledUnloadCheckRegistration;

        protected override void EndDelayedLoad(Drawable content)
        {
            base.EndDelayedLoad(content);

            // Scheduled for another frame since Update() may not have run yet and thus OptimisingContainer may not be up-to-date
            scheduledUnloadCheckRegistration = Game.Schedule(() =>
            {
                // Since this code is running on the game scheduler, it needs to be safe against a potential simultaneous async disposal.
                lock (disposalLock)
                {
                    if (isDisposed)
                        return;

                    // Content must have finished loading, but not necessarily added to the hierarchy.
                    Debug.Assert(DelayedLoadTriggered);
                    Debug.Assert(Content.LoadState >= LoadState.Ready);

                    Debug.Assert(unloadSchedule == null);
                    unloadSchedule = Game.Scheduler.AddDelayed(checkForUnload, 0, true);
                    Debug.Assert(unloadSchedule != null);

                    total_loaded.Value++;
                }
            });
        }

        private readonly object disposalLock = new object();
        private bool isDisposed;

        protected override void Dispose(bool isDisposing)
        {
            lock (disposalLock)
                isDisposed = true;

            base.Dispose(isDisposing);
        }

        protected override void CancelTasks()
        {
            base.CancelTasks();

            if (unloadSchedule != null)
            {
                unloadSchedule.Cancel();
                unloadSchedule = null;

                total_loaded.Value--;
            }

            scheduledUnloadCheckRegistration?.Cancel();
            scheduledUnloadCheckRegistration = null;
        }

        private readonly LayoutValue<IFrameBasedClock> unloadClockBacking = new LayoutValue<IFrameBasedClock>(Invalidation.Parent);

        private IFrameBasedClock unloadClock => unloadClockBacking.IsValid ? unloadClockBacking.Value : (unloadClockBacking.Value = this.FindClosestParent<Game>() == null ? Game.Clock : Clock);

        private void checkForUnload()
        {
            // Since this code is running on the game scheduler, it needs to be safe against a potential simultaneous async disposal.
            lock (disposalLock)
            {
                if (isDisposed)
                    return;

                // Guard against multiple executions of checkForUnload() without an intermediate load having started.
                Debug.Assert(DelayedLoadTriggered);
                Debug.Assert(Content.LoadState >= LoadState.Ready);

                // This code can be expensive, so only run if we haven't yet loaded.
                if (IsIntersecting)
                    timeHidden = 0;
                else
                    timeHidden += unloadClock.ElapsedFrameTime;

                // Don't unload if we don't need to.
                if (!ShouldUnloadContent)
                    return;

                // We need to dispose the content, taking into account what we know at this point in time:
                // 1: The wrapper has not been disposed. Consequently, neither has the content.
                // 2: The content has finished loading.
                // 3: The content may not have been added to the hierarchy (e.g. if this wrapper is hidden). This is dependent upon the value of DelayedLoadCompleted.
                if (DelayedLoadCompleted)
                {
                    Debug.Assert(Content.LoadState >= LoadState.Ready);
                    ClearInternal(); // Content added, remove AND dispose.
                }
                else
                {
                    Debug.Assert(Content.LoadState == LoadState.Ready);
                    DisposeChildAsync(Content); // Content not added, only need to dispose.
                }

                Content = null;
                timeHidden = 0;

                // This has two important roles:
                // 1. Stopping this delegate from executing multiple times.
                // 2. If DelayedLoadCompleted = false (content not yet added to hierarchy), prevents the now disposed content from being added (e.g. if this wrapper becomes visible again).
                CancelTasks();

                // And finally, allow another load to take place.
                DelayedLoadTriggered = DelayedLoadCompleted = false;
            }
        }
    }
}
