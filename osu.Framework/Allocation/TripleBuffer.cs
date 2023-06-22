// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Handles triple-buffering of any object type.
    /// Thread safety assumes at most one writer and one reader.
    /// Comes with the added assurance that the most recent <see cref="GetForRead"/> object is not written to.
    /// </summary>
    public class TripleBuffer<T>
        where T : class
    {
        private readonly ObjectUsage<T>[] buffers = new ObjectUsage<T>[buffer_count];

        /// <summary>
        /// The freshest buffer index which has finished a write, and is waiting to be read.
        /// Will be set to <c>null</c> after being read once.
        /// </summary>
        private int? pendingCompletedWriteIndex;

        /// <summary>
        /// The last buffer index which was obtained for writing.
        /// </summary>
        private int? lastWriteIndex;

        /// <summary>
        /// The last buffer index which was obtained for reading.
        /// Note that this will remain "active" even after a <see cref="GetForRead"/> ends, to give benefit of doubt that the usage may still be accessing it.
        /// </summary>
        private int? lastReadIndex;

        private readonly ManualResetEventSlim writeCompletedEvent = new ManualResetEventSlim();

        private const int buffer_count = 3;

        public TripleBuffer()
        {
            for (int i = 0; i < buffer_count; i++)
                buffers[i] = new ObjectUsage<T>(i, finishUsage);
        }

        public ObjectUsage<T> GetForWrite()
        {
            // Only one write should be allowed at once
            Debug.Assert(buffers.All(b => b.Usage != UsageType.Write));

            ObjectUsage<T> buffer;

            lock (buffers)
            {
                buffer = getNextWriteBuffer();

                Debug.Assert(buffer.Usage == UsageType.None);
                buffer.Usage = UsageType.Write;
            }

            return buffer;
        }

        public ObjectUsage<T>? GetForRead()
        {
            // Only one read should be allowed at once
            Debug.Assert(buffers.All(b => b.Usage != UsageType.Read));

            writeCompletedEvent.Reset();

            lock (buffers)
            {
                if (pendingCompletedWriteIndex != null)
                {
                    var buffer = buffers[pendingCompletedWriteIndex.Value];
                    pendingCompletedWriteIndex = null;
                    buffer.Usage = UsageType.Read;

                    Debug.Assert(lastReadIndex != buffer.Index);
                    lastReadIndex = buffer.Index;
                    return buffer;
                }
            }

            // A completed write wasn't available, so wait for the next to complete.
            if (!writeCompletedEvent.Wait(100))
                // Generally shouldn't happen, but this avoids spinning forever.
                return null;

            return GetForRead();
        }

        private ObjectUsage<T> getNextWriteBuffer()
        {
            for (int i = 0; i < buffer_count; i++)
            {
                // Never write to the last read index.
                // We assume there could be some reads still occurring even after the usage is finished.
                if (i == lastReadIndex) continue;

                // Never write to the same buffer twice in a row.
                // This would defeat the purpose of having a triple buffer.
                if (i == lastWriteIndex) continue;

                lastWriteIndex = i;
                return buffers[i];
            }

            throw new InvalidOperationException("No buffer could be obtained. This should never ever happen.");
        }

        private void finishUsage(ObjectUsage<T> obj)
        {
            lock (buffers)
            {
                switch (obj.Usage)
                {
                    case UsageType.Write:
                        Debug.Assert(pendingCompletedWriteIndex != obj.Index);
                        pendingCompletedWriteIndex = obj.Index;

                        writeCompletedEvent.Set();
                        break;
                }

                obj.Usage = UsageType.None;
            }
        }
    }
}
