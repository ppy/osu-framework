// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Backends
{
    /// <summary>
    /// Abstract base class for <see cref="IBackend"/>s that implements the
    /// <see cref="IDisposable"/> pattern.
    /// </summary>
    public abstract class Backend : IBackend
    {
        protected IGameHost Host { get; private set; }

        public virtual void Initialise(IGameHost host)
        {
            Host = host;
        }

        #region IDisposable

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Backend()
        {
            Dispose(false);
        }

        #endregion
    }
}
