// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Threading;

#nullable enable

namespace osu.Framework.Threading
{
    /// <summary>
    /// A synchronisation context which posts all continuatiuons to a scheduler instance.
    /// </summary>
    internal class SchedulerSynchronizationContext : SynchronizationContext
    {
        private readonly Scheduler scheduler;

        public SchedulerSynchronizationContext(Scheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            var del = scheduler.Add(() => d(state));

            Debug.Assert(del != null);

            while (del.State == ScheduledDelegate.RunState.Waiting)
                scheduler.Update();
        }

        public override void Post(SendOrPostCallback d, object? state) => scheduler.Add(() => d(state));
    }
}
