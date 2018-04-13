// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using osu.Framework.Platform;
using System.Collections.Generic;
using osu.Framework.Configuration;

namespace osu.Framework.Input.Handlers
{
    public abstract class InputHandler : IDisposable
    {
        /// <summary>
        /// Used to initialize resources specific to this InputHandler. It gets called once.
        /// </summary>
        /// <returns>Success of the initialization.</returns>
        public abstract bool Initialize(GameHost host);

        protected ConcurrentQueue<InputState> PendingStates = new ConcurrentQueue<InputState>();

        private readonly object pendingStatesRetrievalLock = new object();

        /// <summary>
        /// Retrieve a list of all pending states since the last call to this method.
        /// </summary>
        public virtual List<InputState> GetPendingStates()
        {
            lock (pendingStatesRetrievalLock)
            {
                List<InputState> pending = new List<InputState>();

                while (PendingStates.TryDequeue(out InputState s))
                    pending.Add(s);

                return pending;
            }
        }

        /// <summary>
        /// Indicates whether this InputHandler is currently delivering input by the user. When handling input the OsuGame uses the first InputHandler which is active.
        /// </summary>
        public abstract bool IsActive { get; }

        /// <summary>
        /// Indicated how high of a priority this handler has. The active handler with the highest priority is controlling the cursor at any given time.
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// Whether this InputHandler should be collecting <see cref="InputState"/>s to return on the next <see cref="GetPendingStates"/> call
        /// </summary>
        public readonly BindableBool Enabled = new BindableBool(true);

        public override string ToString() => GetType().Name;

        #region IDisposable Support

        protected bool IsDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            Enabled.Value = false;
            IsDisposed = true;
        }

        ~InputHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class InputHandlerComparer : IComparer<InputHandler>
    {
        public int Compare(InputHandler h1, InputHandler h2)
        {
            if (h1 == null) throw new ArgumentNullException(nameof(h1));
            if (h2 == null) throw new ArgumentNullException(nameof(h2));

            return h2.Priority.CompareTo(h1.Priority);
        }
    }
}
