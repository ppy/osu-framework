// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Handles triple-buffering of any object type.
    /// Thread safety assumes at most one writer and one reader.
    /// </summary>
    public class TripleBuffer<T>
        where T : class
    {
        private readonly ObjectUsage<T>[] buffers = new ObjectUsage<T>[3];

        private int? lastCompletedWriteIndex;

        private int? activeReadIndex;

        private readonly ManualResetEventSlim writeCompletedEvent = new ManualResetEventSlim();

        private const int buffer_count = 3;

        public TripleBuffer()
        {
            for (int i = 0; i < buffer_count; i++)
                buffers[i] = new ObjectUsage<T>(i, finishUsage);
        }

        public ObjectUsage<T> GetForWrite()
        {
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
            writeCompletedEvent.Reset();

            lock (buffers)
            {
                if (lastCompletedWriteIndex != null)
                {
                    var buffer = buffers[lastCompletedWriteIndex.Value];
                    lastCompletedWriteIndex = null;
                    buffer.Usage = UsageType.Read;

                    Debug.Assert(activeReadIndex == null);
                    activeReadIndex = buffer.Index;
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
            for (int i = 0; i < buffer_count - 1; i++)
            {
                if (i == activeReadIndex) continue;
                if (i == lastCompletedWriteIndex) continue;

                return buffers[i];
            }

            return buffers[buffer_count - 1];
        }

        private void finishUsage(ObjectUsage<T> obj)
        {
            lock (buffers)
            {
                switch (obj.Usage)
                {
                    case UsageType.Read:
                        Debug.Assert(activeReadIndex != null);
                        activeReadIndex = null;
                        break;

                    case UsageType.Write:
                        Debug.Assert(lastCompletedWriteIndex != obj.Index);
                        lastCompletedWriteIndex = obj.Index;

                        writeCompletedEvent.Set();
                        break;
                }

                obj.Usage = UsageType.None;
            }
        }
    }
}
