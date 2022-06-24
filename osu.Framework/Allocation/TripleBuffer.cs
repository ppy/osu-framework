// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

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
        private int? activeWriteIndex;

        public TripleBuffer()
        {
            for (int i = 0; i < 3; i++)
                buffers[i] = new ObjectUsage<T>(i, finish);
        }

        public ObjectUsage<T> GetForWrite()
        {
            Debug.Assert(activeWriteIndex == null);

            lock (buffers)
            {
                var buffer = getNextWriteBuffer();

                activeWriteIndex = buffer.Index;

                buffer.Usage = UsageType.Write;
                buffer.ResetEvent.Reset();

                return buffer;
            }
        }

        public ObjectUsage<T>? GetForRead()
        {
            Debug.Assert(activeReadIndex == null);

            if (lastCompletedWriteIndex == null) return null;

            ObjectUsage<T>? buffer;

            lock (buffers)
            {
                buffer = buffers[lastCompletedWriteIndex.Value];

                if (buffer.Consumed)
                    buffer = getNextWriteBuffer();

                activeReadIndex = buffer.Index;
            }

            buffer.ResetEvent.Wait(1000);
            buffer.Usage = UsageType.Read;

            return buffer;
        }

        private ObjectUsage<T> getNextWriteBuffer()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == activeReadIndex)
                    continue;

                if (i == activeWriteIndex)
                    continue;

                if (i == lastCompletedWriteIndex)
                    continue;

                return buffers[i];
            }

            throw new InvalidOperationException();
        }

        private void finish(ObjectUsage<T> obj, UsageType type)
        {
            switch (type)
            {
                case UsageType.Read:
                    obj.Consumed = true;
                    obj.Usage = UsageType.None;

                    Debug.Assert(activeReadIndex != null);
                    activeReadIndex = null;
                    break;

                case UsageType.Write:
                    obj.Usage = UsageType.None;
                    obj.Consumed = false;
                    obj.ResetEvent.Set();

                    Debug.Assert(lastCompletedWriteIndex != obj.Index);
                    lastCompletedWriteIndex = obj.Index;

                    Debug.Assert(activeWriteIndex != null);
                    activeWriteIndex = null;
                    break;
            }
        }
    }
}
