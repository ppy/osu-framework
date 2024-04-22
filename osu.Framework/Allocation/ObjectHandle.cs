// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Wrapper on <see cref="GCHandle" /> that supports the <see cref="IDisposable" /> pattern.
    /// </summary>
    public struct ObjectHandle<T> : IDisposable
        where T : class
    {
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

        private readonly bool canFree;

        /// <summary>
        /// Wraps the provided object with a <see cref="GCHandle" />, using the given <see cref="GCHandleType" />.
        /// </summary>
        /// <param name="target">The target object to wrap.</param>
        /// <param name="handleType">The handle type to use.</param>
        public ObjectHandle(T target, GCHandleType handleType)
        {
            handle = GCHandle.Alloc(target, handleType);
            canFree = true;
        }

        /// <summary>
        /// Recreates an <see cref="ObjectHandle{T}" /> based on the passed <see cref="IntPtr" />.
        /// If <paramref name="ownsHandle"/> is <c>true</c>, disposing this object will free the handle.
        /// </summary>
        /// <param name="handle"><see cref="Handle"/> from a previously constructed <see cref="ObjectHandle{T}(T, GCHandleType)"/>.</param>
        /// <param name="ownsHandle">Whether this instance owns the underlying <see cref="GCHandle"/>.</param>
        public ObjectHandle(IntPtr handle, bool ownsHandle = false)
        {
            this.handle = GCHandle.FromIntPtr(handle);
            canFree = ownsHandle;
        }

        /// <summary>
        /// Gets the object being referenced.
        /// Returns true if successful and populates <paramref name="target"/> with the referenced object.
        /// Returns false If the handle is not allocated or the target is not of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="target">Populates this parameter with the targeted object.</param>
        public bool GetTarget(out T target)
        {
            if (!handle.IsAllocated)
            {
                target = default;
                return false;
            }

            try
            {
                if (handle.Target is T value)
                {
                    target = value;
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
            }

            target = default;
            return false;
        }

        #region IDisposable Support

        public void Dispose()
        {
            if (canFree && handle.IsAllocated)
                handle.Free();
        }

        #endregion
    }
}
