//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Allocation
{
    public class BufferStack
    {
        private int maxAmountBuffers;
        private Stack<byte[]> freeDataBuffers = new Stack<byte[]>();
        private HashSet<byte[]> usedDataBuffers = new HashSet<byte[]>();

        public BufferStack(int maxAmountBuffers = 10)
        {
            this.maxAmountBuffers = maxAmountBuffers;
        }

        private byte[] findFreeBuffer(int minimumLength)
        {
            byte[] buffer = null;

            if (freeDataBuffers.Count > 0)
                buffer = freeDataBuffers.Pop();

            if (buffer == null || buffer.Length < minimumLength)
                buffer = new byte[minimumLength];

            if (usedDataBuffers.Count < maxAmountBuffers)
                usedDataBuffers.Add(buffer);

            return buffer;
        }

        private void returnFreeBuffer(byte[] buffer)
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
        public byte[] ReserveBuffer(int minimumLength)
        {
            byte[] buffer;
            lock (freeDataBuffers)
                buffer = findFreeBuffer(minimumLength);

            return buffer;
        }

        /// <summary>
        /// Frees a previously reserved buffer for future reservations.
        /// </summary>
        /// <param name="buffer">The buffer to be freed. If the buffer has not previously been reserved then this method does nothing.</param>
        public void FreeBuffer(byte[] buffer)
        {
            lock (freeDataBuffers)
                returnFreeBuffer(buffer);
        }
    }
}
