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
        protected ConcurrentQueue<Delegate> PendingActions = new ConcurrentQueue<Delegate>();

        protected SeekAction LastSeekAction;

        protected delegate void SeekAction();

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

            Delegate actionItem;
            while (!IsDisposed && PendingActions.TryDequeue(out actionItem))
            {
                if (actionItem is Action)
                    ((Action)actionItem)();
                else if (actionItem is SeekAction)
                {
                    SeekAction seekActionItem = (SeekAction)actionItem;
                    if (seekActionItem == LastSeekAction)
                        seekActionItem();
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
            PendingActions.Enqueue(new Action(() => Dispose(true)));
        }

        #endregion
    }
}
