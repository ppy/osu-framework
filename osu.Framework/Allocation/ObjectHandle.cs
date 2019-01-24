// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Wrapper on <see cref="GCHandle" /> that supports the <see cref="IDisposable" /> pattern.
    /// </summary>
    public struct ObjectHandle<T> : IDisposable
    {
        /// <summary>
        /// The object being referenced.  Returns the default value for <see cref="T" /> if the handle is not allocated
        /// or if the handle points to an object that cannot be cast to <see cref="T" />.
        /// </summary>
        public T Target => handle.IsAllocated && handle.Target is T ? (T)handle.Target : default;

        /// <summary>
        /// The pointer from the <see cref="GCHandle" />, if it is allocated.  Otherwise <see cref="IntPtr.Zero" />.
        /// </summary>
        public IntPtr Handle => handle.IsAllocated ? GCHandle.ToIntPtr(handle) : IntPtr.Zero;

        /// <summary>
        /// The address of target object, if it is allocated and pinned.  Otherwise <see cref="IntPtr.Zero" />.
        /// Note: This is not the same as the <see cref="Handle" />.
        /// </summary>
        public IntPtr Address => handle.IsAllocated ? handle.AddrOfPinnedObject() : IntPtr.Zero;

        public bool IsAllocated => handle.IsAllocated;

        private GCHandle handle;

        private readonly bool fromPointer;

        /// <summary>
        /// Wraps the provided object with a <see cref="GCHandle" />, using the given <see cref="GCHandleType" />.
        /// </summary>
        /// <param name="target">The target object to wrap.</param>
        /// <param name="handleType">The handle type to use.</param>
        public ObjectHandle(T target, GCHandleType handleType)
        {
            handle = GCHandle.Alloc(target, handleType);
            fromPointer = false;
        }

        /// <summary>
        /// Recreates an <see cref="ObjectHandle{T}" /> based on the passed <see cref="IntPtr" />.
        /// Disposing this object will not free the handle, the original object must be disposed instead.
        /// </summary>
        /// <param name="handle">Handle.</param>
        public ObjectHandle(IntPtr handle)
        {
            this.handle = GCHandle.FromIntPtr(handle);
            fromPointer = true;
        }

        #region IDisposable Support

        public void Dispose()
        {
            if (!fromPointer && handle.IsAllocated)
                handle.Free();
        }

        #endregion
    }
}
