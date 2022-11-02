// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Input.Handlers
{
    public abstract class InputHandler : IDisposable, IHasDescription
    {
        private static readonly Logger logger = Logger.GetLogger(LoggingTarget.Input);

        private bool isInitialized;

        /// <summary>
        /// Used to initialize resources specific to this InputHandler. It gets called once.
        /// </summary>
        /// <returns>Success of the initialization.</returns>
        public virtual bool Initialize(GameHost host)
        {
            if (isInitialized)
                throw new InvalidOperationException($"{nameof(Initialize)} was run more than once");

            isInitialized = true;
            return true;
        }

        /// <summary>
        /// Reset this handler to a sane default state. This should reset any settings a consumer or user may have changed in order to attempt to make the handler usable again.
        /// </summary>
        /// <remarks>
        /// An example would be a user setting the sensitivity too high to turn it back down, or restricting the navigable screen area too small.
        /// Calling this would attempt to return the user to a sane state so they could re-attempt configuration changes.
        /// </remarks>
        public virtual void Reset()
        {
        }

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
        /// A user-readable description of this input handler, for display in settings and logs.
        /// </summary>
        public virtual string Description => ToString().Replace("Handler", string.Empty);

        /// <summary>
        /// Whether this InputHandler should be collecting <see cref="IInput"/>s to return on the next <see cref="CollectPendingInputs"/> call
        /// </summary>
        public BindableBool Enabled { get; } = new BindableBool(true);

        /// <summary>
        /// Logs an arbitrary string prefixed by the name of this input handler.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="level">The verbosity level.</param>
        /// <param name="exception">An optional related exception.</param>
        protected void Log(string message, LogLevel level = LogLevel.Verbose, Exception exception = null) => logger.Add($"[{Description}] {message}", level, exception);

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
