﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Logging;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A stack of buffers (arrays with elements of type <see cref="T"/>) which allows bypassing the
    /// garbage collector and expensive allocations when buffers can be frequently re-used.
    /// The stack nature ensures that the most recently used buffers remain hot in memory, while
    /// at the same time guaranteeing a certain degree of order preservation.
    /// </summary>
    public class BufferStack<T>
    {
        private readonly int maxAmountBuffers;
        private readonly Stack<T[]> freeDataBuffers = new Stack<T[]>();
        private readonly HashSet<T[]> usedDataBuffers = new HashSet<T[]>();

        /// <summary>
        /// The number of buffers currently in use.
        /// </summary>
        public int BuffersInUse
        {
            get
            {
                lock (freeDataBuffers)
                    return totalUsage;
            }
        }

        /// <summary>
        /// Creates a new buffer stack containing a given maximum amount of buffers.
        /// </summary>
        /// <param name="maxAmountBuffers">The maximum amount of buffers to be contained within the buffer stack.</param>
        public BufferStack(int maxAmountBuffers)
        {
            this.maxAmountBuffers = maxAmountBuffers;
        }

        private int totalUsage;

        private T[] findFreeBuffer(int minimumLength)
        {
            T[] buffer = null;

            totalUsage++;

            if (freeDataBuffers.Count > 0)
                buffer = freeDataBuffers.Pop();

            if (buffer == null || buffer.Length < minimumLength)
                buffer = new T[minimumLength];

            if (usedDataBuffers.Count < maxAmountBuffers)
                usedDataBuffers.Add(buffer);

            if (totalUsage > maxAmountBuffers)
                Logger.Log($"BufferStack usage exceeded allocation (usage: {totalUsage} max: {maxAmountBuffers})", LoggingTarget.Performance);

            return buffer;
        }

        private void returnFreeBuffer(T[] buffer)
        {
            totalUsage--;

            if (usedDataBuffers.Remove(buffer))
                // We are here if the element was successfully found and removed
                freeDataBuffers.Push(buffer);
        }

        /// <summary>
        /// Reserve a buffer from the buffer stack. If no free buffers are available, a new one is allocated.
        /// </summary>
        /// <param name="minimumLength">The minimum length required of the reserved buffer.</param>
        /// <returns>The reserved buffer.</returns>
        public T[] ReserveBuffer(int minimumLength)
        {
            T[] buffer;
            lock (freeDataBuffers)
                buffer = findFreeBuffer(minimumLength);

            return buffer;
        }

        /// <summary>
        /// Frees a previously reserved buffer for future reservations.
        /// </summary>
        /// <param name="buffer">The buffer to be freed. If the buffer has not previously been reserved then this method does nothing.</param>
        public void FreeBuffer(T[] buffer)
        {
            lock (freeDataBuffers)
                returnFreeBuffer(buffer);
        }
    }
}
