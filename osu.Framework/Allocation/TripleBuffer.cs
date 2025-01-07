// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Handles triple-buffering of any object type.
    /// Thread safety assumes at most one writer and one reader.
    /// Comes with the added assurance that the most recent <see cref="GetForRead"/> object is not written to.
    /// </summary>
    internal class TripleBuffer<T>
        where T : class
    {
        /// <summary>
        /// The default amount of time (in milliseconds) to wait for a write to occur during <see cref="GetForRead"/>.
        /// </summary>
        public const int DEFAULT_READ_TIMEOUT = 100;

        private const int buffer_count = 3;

        private readonly Buffer[] buffers = new Buffer[buffer_count];
        private readonly Stopwatch stopwatch = new Stopwatch();

        private int writeIndex;
        private int flipIndex = 1;
        private int readIndex = 2;

        public TripleBuffer()
        {
            for (int i = 0; i < buffer_count; i++)
                buffers[i] = new Buffer(i, finishUsage);
        }

        /// <summary>
        /// Retrieves a buffer to be written to.
        /// </summary>
        /// <returns>The buffer.</returns>
        public Buffer GetForWrite()
        {
            Buffer usage = buffers[writeIndex];
            usage.LastUsage = UsageType.Write;
            return usage;
        }

        /// <summary>
        /// Attempts to retrieve a buffer to read.
        /// </summary>
        /// <param name="timeout">Amount of time (in milliseconds) to wait for a buffer to be written.</param>
        /// <returns>An available buffer to be read, or <c>null</c> if no buffer has been written.</returns>
        public Buffer? GetForRead(int timeout = DEFAULT_READ_TIMEOUT)
        {
            stopwatch.Restart();

            while (true)
            {
                flip(ref readIndex);

                if (buffers[readIndex].LastUsage != UsageType.Read)
                    break;

                if (timeout == 0 || stopwatch.ElapsedMilliseconds > timeout)
                    return null;
            }

            Buffer usage = buffers[readIndex];

            Debug.Assert(usage.LastUsage == UsageType.Write);
            usage.LastUsage = UsageType.Read;

            return usage;
        }

        private void finishUsage(Buffer usage)
        {
            if (usage.LastUsage == UsageType.Write)
                flip(ref writeIndex);
        }

        private void flip(ref int localIndex)
        {
            localIndex = Interlocked.Exchange(ref flipIndex, localIndex);
        }

        public class Buffer : IDisposable
        {
            public T? Object;

            public volatile UsageType LastUsage;

            public readonly int Index;

            private readonly Action<Buffer>? finish;

            public Buffer(int index, Action<Buffer>? finish)
            {
                Index = index;
                this.finish = finish;
            }

            public void Dispose()
            {
                finish?.Invoke(this);
            }
        }

        public enum UsageType
        {
            Read,
            Write
        }
    }
}
