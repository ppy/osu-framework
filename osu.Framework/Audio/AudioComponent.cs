// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using osu.Framework.DebugUtils;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    public class AudioComponent : IDisposable, IUpdateable
    {
        /// <summary>
        /// Audio operations will be run on a separate dedicated thread, so we need to schedule any audio API calls using this queue.
        /// </summary>
        protected ConcurrentQueue<Action> PendingActions = new ConcurrentQueue<Action>();

        ~AudioComponent()
        {
            Dispose(false);
        }

        private event Action onLoaded;

        private readonly object loadedLock = new object();

        /// <summary>
        /// Executes an action as soon as this audio component is loaded. If this component is already loaded, the action is executed on the next update.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public void RunWhenLoaded(Action action)
        {
            lock (loadedLock)
                onLoaded += action;
        }

        /// <summary>
        /// Updates this audio component. Always runs on the audio thread.
        /// </summary>
        public virtual void Update()
        {
            ThreadSafety.EnsureNotUpdateThread();
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not update disposed audio components.");

            FrameStatistics.Add(StatisticsCounterType.TasksRun, PendingActions.Count);
            FrameStatistics.Increment(StatisticsCounterType.Components);

            Action action;
            while (!IsDisposed && PendingActions.TryDequeue(out action))
                action();

            // Perform all OnLoaded actions if there is need to.
            if (IsLoaded && onLoaded != null)
            {
                lock (loadedLock)
                {
                    onLoaded();
                    onLoaded = null;
                }
            }
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
            PendingActions.Enqueue(() => Dispose(true));
        }

        #endregion
    }
}
