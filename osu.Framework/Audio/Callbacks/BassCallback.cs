// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Abstract class that provides an optional pinned <see cref="ObjectHandle{T}" />, used for callbacks to
    /// the Bass library.  Targets that do not support JIT compiling (such as iOS) can subclass implementations
    /// to use static callbacks.
    /// </summary>
    public abstract class BassCallback : IDisposable
    {
        private readonly ObjectHandle<BassCallback> pinnedHandle;

        protected BassCallback()
        {
            if (ShouldPin)
                pinnedHandle = new ObjectHandle<BassCallback>(this, GCHandleType.Pinned);
        }

        /// <summary>
        /// The pinned handle, or <see cref="IntPtr.Zero" /> if the object is not pinned.
        /// </summary>
        public IntPtr Handle => pinnedHandle.Handle;

        /// <summary>
        /// Returns true if the callback should be pinned on creation.  Defaults to false, may be overridden by
        /// platform-specific implementations.
        /// </summary>
        protected virtual bool ShouldPin => false;

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                pinnedHandle.Dispose();
                disposedValue = true;
            }
        }

        ~BassCallback()
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
