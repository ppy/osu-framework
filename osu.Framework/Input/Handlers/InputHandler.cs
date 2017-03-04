﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using osu.Framework.Platform;
using System.Collections.Generic;

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

        public virtual List<InputState> GetPendingStates()
        {
            lock (this)
            {
                List<InputState> pending = new List<InputState>();

                InputState s;
                while (PendingStates.TryDequeue(out s))
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

        #region IDisposable Support
        protected bool IsDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
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
            return h2.Priority.CompareTo(h1.Priority);
        }
    }
}
