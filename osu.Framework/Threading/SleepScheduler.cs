// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

        public override bool Add(Action d, bool forceDelayed = true)
        {
            if (!sleeper.IsSleeping || IsMainThread)
            {
                base.Add(d, forceDelayed);
                return true;
            }
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (sleeper.IsSleeping)
                    sleeper.Invoke(d);
                else
                    Add(d, forceDelayed);
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
