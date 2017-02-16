// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;

namespace osu.Framework.Threading
{
    public class SleepScheduler : Scheduler
    {
        private SleepHandle sleeper;

        public SleepScheduler(SleepHandle sleeper)
        {
            this.sleeper = sleeper;
        }

        public override bool Add(Action task, bool forceScheduled = true)
        {
            if (!sleeper.IsSleeping || IsMainThread)
            {
                base.Add(task, forceScheduled);
                return true;
            }
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (sleeper.IsSleeping)
                    sleeper.Invoke(task);
                else
                    Add(task, forceScheduled);
            });

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            sleeper?.Dispose();
            sleeper = null;
        }
    }
}
