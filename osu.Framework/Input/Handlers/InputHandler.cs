// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using osu.Framework.Platform;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;

namespace osu.Framework.Input.Handlers
{
    public abstract class InputHandler : IDisposable
    {
        /// <summary>
        /// Used to initialize resources specific to this InputHandler. It gets called once.
        /// </summary>
        /// <returns>Success of the initialization.</returns>
        public abstract bool Initialize(GameHost host);

        protected ConcurrentQueue<IInput> PendingInputs = new ConcurrentQueue<IInput>();

        private readonly object pendingInputsRetrievalLock = new object();

        /// <summary>
        /// Add all pending states since the last call to this method to a provided list.
        /// </summary>
        /// <param name="inputs">The list for pending inputs to be added to.</param>
        public virtual void CollectPendingInputs(List<IInput> inputs)
        {
            lock (pendingInputsRetrievalLock)
            {
                while (PendingInputs.TryDequeue(out IInput s))
                    inputs.Add(s);
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
        /// Whether this InputHandler should be collecting <see cref="IInput"/>s to return on the next <see cref="CollectPendingInputs"/> call
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
}
