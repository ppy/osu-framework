// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Threading;

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
        }

        private double timeHidden;

        private ScheduledDelegate unloadSchedule;

        protected bool ShouldUnloadContent => timeBeforeUnload == 0 || timeHidden > timeBeforeUnload;

        public override double LifetimeStart
        {
            get => base.Content?.LifetimeStart ?? double.MinValue;
            set => throw new NotSupportedException();
        }

        public override double LifetimeEnd
        {
            get => base.Content?.LifetimeEnd ?? double.MaxValue;
            set => throw new NotSupportedException();
        }

        public override Drawable Content => base.Content ?? (Content = createContentFunction());

        protected override void EndDelayedLoad(Drawable content)
        {
            base.EndDelayedLoad(content);
            Debug.Assert(unloadSchedule == null);
            unloadSchedule = OptimisingContainer?.ScheduleCheckAction(checkForUnload);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            unloadSchedule?.Cancel();
            unloadSchedule = null;
        }

        private void checkForUnload()
        {
            // This code can be expensive, so only run if we haven't yet loaded.
            if (IsIntersecting)
                timeHidden = 0;
            else
                timeHidden += Time.Elapsed;

            if (ShouldUnloadContent)
            {
                ClearInternal();
                Content = null;

                timeHidden = 0;

                unloadSchedule?.Cancel();
                unloadSchedule = null;

                CancelTasks();
            }
        }
    }
}
