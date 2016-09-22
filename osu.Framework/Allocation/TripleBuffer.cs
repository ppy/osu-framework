﻿using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Handles triple-buffering of any object type.
    /// Thread safety assumes at most one writer and one reader.
    /// </summary>
    public class TripleBuffer<T>
    {
        private ObjectUsage<T>[] buffers = new ObjectUsage<T>[3];

        int read;
        int write;
        int lastWrite = -1;

        Action<ObjectUsage<T>, UsageType> finishDelegate;

        public TripleBuffer()
        {
            //caching the delegate means we only have to allocate it once, rather than once per created buffer.
            finishDelegate = finish;
        }

        public ObjectUsage<T> Get(UsageType usage)
        {
            switch (usage)
            {
                case UsageType.Write:
                    lock (buffers)
                    {
                        while ((buffers[write]?.Usage == UsageType.Read) || write == lastWrite)
                            write = (write + 1) % 3;
                    }

                    if (buffers[write] == null)
                    {
                        buffers[write] = new ObjectUsage<T>
                        {
                            Finish = finishDelegate,
                            Usage = UsageType.Write
                        };
                    }
                    else
                    {
                        buffers[write].Usage = UsageType.Write;
                    }

                    return buffers[write];
                case UsageType.Read:
                    if (lastWrite < 0) return null;

                    lock (buffers)
                    {
                        read = lastWrite;
                        buffers[read].Usage = UsageType.Read;
                    }

                    return buffers[read];

            }

            return null;
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
