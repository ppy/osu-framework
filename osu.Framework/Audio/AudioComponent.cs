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
        protected ConcurrentQueue<ExtendedAction> PendingActions = new ConcurrentQueue<ExtendedAction>();

        protected ExtendedAction lastSeekAction;

        ~AudioComponent()
        {
            Dispose(false);
        }

        /// <summary>
        /// Updates this audio component. Always runs on the audio thread.
        /// </summary>
        public virtual void Update()
        {
            ThreadSafety.EnsureNotUpdateThread();
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not update disposed audio components.");

            FrameStatistics.Increment(StatisticsCounterType.TasksRun, PendingActions.Count);
            FrameStatistics.Increment(StatisticsCounterType.Components);

            ExtendedAction extendedAction;
            while (!IsDisposed && PendingActions.TryDequeue(out extendedAction))
            {
                if (extendedAction.IsSeekAction && extendedAction != lastSeekAction)
                    continue;

                extendedAction.Action();
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
            PendingActions.Enqueue(new ExtendedAction(() => Dispose(true)));
        }

        #endregion

        public class ExtendedAction
        {
            public Action Action;

            public bool IsSeekAction;

            public ExtendedAction(Action Action, bool isSeekAction = false)
            {
                this.Action = Action;
                IsSeekAction = isSeekAction;
            }
        }
    }
}
