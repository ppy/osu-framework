// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Threading;

#nullable enable

namespace osu.Framework.Threading
{
    /// <summary>
    /// A synchronisation context which posts all continuations to a scheduler instance.
    /// </summary>
    internal class GameThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Scheduler scheduler;

        public int TotalTasksRun => scheduler.TotalTasksRun;

        public GameThreadSynchronizationContext(GameThread gameThread)
        {
            scheduler = new GameThreadScheduler(gameThread);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            var del = scheduler.Add(() => d(state));

            Debug.Assert(del != null);

            while (del.State == ScheduledDelegate.RunState.Waiting)
            {
                if (scheduler.IsMainThread)
                    scheduler.Update();
                else
                    Thread.Sleep(1);
            }
        }

        public override void Post(SendOrPostCallback d, object? state) => scheduler.Add(() => d(state));

        public void RunWork() => scheduler.Update();
    }
}
