// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    public class AudioComponent : IDisposable, IUpdateable
    {
        /// <summary>
        /// Audio operations will be run on a separate dedicated thread, so we need to schedule any audio API calls using this queue.
        /// </summary>
        protected ConcurrentQueue<Action> PendingActions = new ConcurrentQueue<Action>();

        /// <summary>
        /// Enqueue an action to be run on the audio thread queue (<see cref="PendingActions"/>).
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="waitUntilComplete">Whether to block until the action has been completed. Useful to avoid threading issues for critical tasks.</param>
        protected void EnqueueAction(Action action, bool waitUntilComplete = false)
        {
            var usableAction = action;

            bool hasCompleted = false;
            if (waitUntilComplete)
            {
                usableAction = () =>
                {
                    action();
                    hasCompleted = true;
                };
            }

            PendingActions.Enqueue(usableAction);

            if (waitUntilComplete)
                while (!hasCompleted)
                    Thread.Sleep(1);
        }

        ~AudioComponent()
        {
            Dispose(false);
        }

        /// <summary>
        /// Run each loop of the audio thread after queued actions to allow components to update anything they need to.
        /// </summary>
        protected virtual void UpdateState()
        {
        }

        /// <summary>
        /// Updates this audio component. Always runs on the audio thread.
        /// </summary>
        public void Update()
        {
            ThreadSafety.EnsureNotUpdateThread();
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not update disposed audio components.");

            FrameStatistics.Add(StatisticsCounterType.TasksRun, PendingActions.Count);
            FrameStatistics.Increment(StatisticsCounterType.Components);

            Action action;
            while (!IsDisposed && PendingActions.TryDequeue(out action))
                action();

            if (!IsDisposed)
                UpdateState();
        }

        public virtual bool HasCompleted => IsDisposed;

        public virtual bool IsLoaded => true;

        #region IDisposable Support

        protected volatile bool IsDisposed; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            EnqueueAction(() => Dispose(true));
        }

        #endregion
    }
}
