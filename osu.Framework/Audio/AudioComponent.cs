// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using osu.Framework.DebugUtils;

namespace osu.Framework.Audio
{
    public class AudioComponent : IDisposable, IUpdateable, IHasCompletedState
    {
        /// <summary>
        /// Audio operations will be run on a separate dedicated thread, so we need to schedule any audio API calls using this queue.
        /// </summary>
        protected ConcurrentQueue<Action> PendingActions = new ConcurrentQueue<Action>();

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
            Debug.Assert(!IsDisposed, "Can not update disposed audio components.");

            Action action;
            while (PendingActions.TryDequeue(out action))
                action();
        }

        public virtual bool HasCompleted => false;

        #region IDisposable Support

        protected bool IsDisposed; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}