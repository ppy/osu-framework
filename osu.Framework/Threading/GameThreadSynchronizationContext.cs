// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;

namespace osu.Framework.Threading
{
    /// <summary>
    /// A synchronisation context which posts all continuations to an isolated scheduler instance.
    /// </summary>
    /// <remarks>
    /// This implementation roughly follows the expectations set out for winforms/WPF as per
    /// https://docs.microsoft.com/en-us/archive/msdn-magazine/2011/february/msdn-magazine-parallel-computing-it-s-all-about-the-synchronizationcontext.
    /// - Calls to <see cref="Post"/> are guaranteed to run asynchronously.
    /// - Calls to <see cref="Send"/> will run inline when they can.
    /// - Order of execution is guaranteed (in our case, it is guaranteed over <see cref="Send"/> and <see cref="Post"/> calls alike).
    /// - To enforce the above, calling <see cref="Send"/> will flush any pending work until the newly queued item has been completed.
    /// </remarks>
    internal class GameThreadSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// The total tasks this synchronization context has run.
        /// </summary>
        public int TotalTasksRun => scheduler?.TotalTasksRun ?? 0;

        private Scheduler? scheduler;

        public GameThreadSynchronizationContext(GameThread gameThread)
        {
            scheduler = new GameThreadScheduler(gameThread);
        }

        public override void Send(SendOrPostCallback callback, object? state)
        {
            var scheduledDelegate = scheduler?.Add(() => callback(state));

            if (scheduledDelegate == null)
                return;

            while (scheduledDelegate.State < ScheduledDelegate.RunState.Complete)
            {
                var runScheduler = scheduler;

                if (runScheduler == null)
                    return;

                if (runScheduler.IsMainThread)
                    runScheduler.Update();
                else
                    Thread.Sleep(1);
            }
        }

        public override void Post(SendOrPostCallback callback, object? state) => scheduler?.Add(() => callback(state));

        /// <summary>
        /// Run any pending work queued against this synchronization context.
        /// </summary>
        public void RunWork() => scheduler?.Update();

        /// <summary>
        /// Disassociate any references to the <see cref="GameThread"/> provided at construction time.
        /// This ensures external components cannot hold a reference to potentially expensive game instances.
        /// </summary>
        public void DisassociateGameThread()
        {
            scheduler = null;
        }
    }
}
