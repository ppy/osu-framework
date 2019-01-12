// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        /// The object being referenced.
        /// </summary>
        public T Target { get; private set; }

        /// <summary>
        /// The pointer from the <see cref="GCHandle" />, if it is allocated.  Otherwise <see cref="IntPtr.Zero" />.
        /// </summary>
        public IntPtr Handle => handle.IsAllocated ? GCHandle.ToIntPtr(handle) : IntPtr.Zero;

        private GCHandle handle;

        /// <summary>
        /// Wraps the provided object with a <see cref="GCHandle" />, using the given <see cref="GCHandleType" />.
        /// </summary>
        /// <param name="target">The target object to wrap.</param>
        /// <param name="handleType">The handle type to use.</param>
        public ObjectHandle(T target, GCHandleType handleType)
        {
            Target = target;
            handle = GCHandle.Alloc(target, handleType);
        }

        /// <summary>
        /// Wrapper on <see cref="GCHandle.FromIntPtr" /> that returns the associated object.
        /// </summary>
        /// <returns>The associated object.</returns>
        /// <param name="handle">The pointer to the associated object.</param>
        public static T FromPointer(IntPtr handle) => (T)GCHandle.FromIntPtr(handle).Target;

        #region IDisposable Support

        public void Dispose()
        {
            if (handle.IsAllocated)
                handle.Free();
        }

        #endregion
    }
}
