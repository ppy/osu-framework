// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        private int read;
        private int write;
        private int lastWrite = -1;

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

        public ObjectUsage<T> Get(UsageType usage)
        {
            switch (usage)
            {
                case UsageType.Write:
                    lock (buffers)
                    {
                        while (buffers[write]?.Usage == UsageType.Read || write == lastWrite)
                            write = (write + 1) % 3;
                    }

                    buffers[write].Usage = UsageType.Write;
                    buffers[write].FrameId = Interlocked.Increment(ref currentFrame);

                    return buffers[write];

                case UsageType.Read:
                    if (lastWrite < 0) return null;

                    lock (buffers)
                    {
                        read = lastWrite;
                        buffers[read].Usage = UsageType.Read;
                    }

                    return buffers[read];

                default:
                    throw new ArgumentOutOfRangeException(nameof(usage), "Unsupported usage type");
            }
        }

        private void finish(ObjectUsage<T> obj, UsageType type)
        {
            switch (type)
            {
                case UsageType.Read:
                    lock (buffers)
                        buffers[read].Usage = UsageType.None;
                    break;

                case UsageType.Write:
                    lock (buffers)
                    {
                        buffers[write].Usage = UsageType.None;
                        lastWrite = write;
                    }

                    break;
            }
        }
    }
}
