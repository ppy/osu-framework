// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Allocation
{
    public class BufferStack<T>
    {
        private readonly int maxAmountBuffers;
        private readonly Stack<T[]> freeDataBuffers = new Stack<T[]>();
        private readonly HashSet<T[]> usedDataBuffers = new HashSet<T[]>();

        public BufferStack(int maxAmountBuffers)
        {
            this.maxAmountBuffers = maxAmountBuffers;
        }

        private T[] findFreeBuffer(int minimumLength)
        {
            T[] buffer = null;

            if (freeDataBuffers.Count > 0)
                buffer = freeDataBuffers.Pop();

            if (buffer == null || buffer.Length < minimumLength)
                buffer = new T[minimumLength];

            if (usedDataBuffers.Count < maxAmountBuffers)
                usedDataBuffers.Add(buffer);

            return buffer;
        }

        private void returnFreeBuffer(T[] buffer)
        {
            if (usedDataBuffers.Remove(buffer))
                // We are here if the element was successfully found and removed
                freeDataBuffers.Push(buffer);
        }

        /// <summary>
        /// Reserve a buffer from the texture buffer pool. This is used to avoid excessive amounts of heap allocations.
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
