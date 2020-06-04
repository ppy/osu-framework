// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        private readonly Func<Drawable> createContentFunction;
        private readonly double timeBeforeUnload;

        public DelayedLoadUnloadWrapper(Func<Drawable> createContentFunction, double timeBeforeLoad = 500, double timeBeforeUnload = 1000)
            : base(createContentFunction(), timeBeforeLoad)
        {
            this.createContentFunction = createContentFunction;
            this.timeBeforeUnload = timeBeforeUnload;

            AddLayout(unloadClockBacking);
        }

        private static readonly GlobalStatistic<int> total_loaded = GlobalStatistics.Get<int>("Drawable", $"{nameof(DelayedLoadUnloadWrapper)}s");

        private double timeHidden;

        private ScheduledDelegate unloadSchedule;

        protected bool ShouldUnloadContent => timeBeforeUnload == 0 || timeHidden > timeBeforeUnload;

        private double lifetimeStart = double.MinValue;

        public override double LifetimeStart
        {
            get => base.Content?.LifetimeStart ?? lifetimeStart;
            set
            {
                if (base.Content != null)
                    base.Content.LifetimeStart = value;
                lifetimeStart = value;
            }
        }

        private double lifetimeEnd = double.MaxValue;

        public override double LifetimeEnd
        {
            get => base.Content?.LifetimeEnd ?? lifetimeEnd;
            set
            {
                if (base.Content != null)
                    base.Content.LifetimeEnd = value;
                lifetimeEnd = value;
            }
        }

        public override Drawable Content => base.Content ?? (Content = createContentFunction());

        private bool contentLoaded;

        protected override void EndDelayedLoad(Drawable content)
        {
            base.EndDelayedLoad(content);

            content.LifetimeStart = lifetimeStart;
            content.LifetimeEnd = lifetimeEnd;

            // Scheduled for another frame since Update() may not have run yet and thus OptimisingContainer may not be up-to-date
            Schedule(() =>
            {
                Debug.Assert(!contentLoaded);
                Debug.Assert(unloadSchedule == null);

                contentLoaded = true;

                unloadSchedule = Game.Scheduler.AddDelayed(checkForUnload, 0, true);
                Debug.Assert(unloadSchedule != null);

                total_loaded.Value++;
            });
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
        }

        private readonly LayoutValue<IFrameBasedClock> unloadClockBacking = new LayoutValue<IFrameBasedClock>(Invalidation.Parent);

        private IFrameBasedClock unloadClock => unloadClockBacking.IsValid ? unloadClockBacking.Value : (unloadClockBacking.Value = FindClosestParent<Game>() == null ? Game.Clock : Clock);

        private void checkForUnload()
        {
            // This code can be expensive, so only run if we haven't yet loaded.
            if (IsIntersecting)
                timeHidden = 0;
            else
                timeHidden += unloadClock.ElapsedFrameTime;

            if (ShouldUnloadContent)
            {
                Debug.Assert(contentLoaded);

                ClearInternal();
                Content = null;

                timeHidden = 0;

                CancelTasks();

                contentLoaded = false;
            }
        }
    }
}
