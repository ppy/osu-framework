// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        private int? activeWriteIndex;

        private long currentFrame;

        public TripleBuffer()
        {
            for (int i = 0; i < 3; i++)
            {
                buffers[i] = new ObjectUsage<T>
                {
                    Finish = finish,
                    Usage = UsageType.Write,
                    Index = i,
                };
            }
        }

        public ObjectUsage<T>? Get(UsageType usage)
        {
            ObjectUsage<T> buffer;

            switch (usage)
            {
                case UsageType.Write:
                    lock (buffers)
                    {
                        activeWriteIndex = getNextWriteBuffer();

                        buffer = buffers[activeWriteIndex.Value];

                        buffer.Usage = UsageType.Write;
                        buffer.FrameId = Interlocked.Increment(ref currentFrame);
                        buffer.ResetEvent.Reset();
                    }

                    break;

                case UsageType.Read:
                    lock (buffers)
                    {
                        if (lastCompletedWriteIndex == null) return null;

                        buffer = buffers[lastCompletedWriteIndex.Value];

                        if (buffer.Consumed)
                            buffer = buffers[getNextWriteBuffer()];
                    }

                    activeReadIndex = buffer.Index;
                    buffer.ResetEvent.Wait(1000);
                    buffer.Usage = UsageType.Read;

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(usage), "Unsupported usage type");
            }

            return buffer;
        }

        private int getNextWriteBuffer()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == activeReadIndex)
                    continue;

                if (i == activeWriteIndex)
                    continue;

                if (i == lastCompletedWriteIndex)
                    continue;

                return i;
            }

            throw new InvalidOperationException();
        }

        private void finish(ObjectUsage<T> obj, UsageType type)
        {
            switch (type)
            {
                case UsageType.Read:
                    lock (buffers)
                    {
                        obj.Usage = UsageType.None;
                        obj.Consumed = true;

                        activeReadIndex = null;
                    }

                    break;

                case UsageType.Write:
                    lock (buffers)
                    {
                        obj.Usage = UsageType.None;
                        obj.ResetEvent.Set();
                        obj.Consumed = false;

                        lastCompletedWriteIndex = obj.Index;
                        activeWriteIndex = null;
                    }

                    break;
            }
        }
    }
}
