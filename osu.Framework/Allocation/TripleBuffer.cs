// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
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
        private const int buffer_count = 3;
        private readonly ObjectUsage<T>[] buffers = new ObjectUsage<T>[buffer_count];

        private int frontIndex;
        private int flipIndex = 1;
        private int backIndex = 2;

        public TripleBuffer()
        {
            for (int i = 0; i < buffer_count; i++)
                buffers[i] = new ObjectUsage<T>(i, finishUsage);
        }

        public ObjectUsage<T> GetForWrite()
        {
            ObjectUsage<T> usage = buffers[frontIndex];
            usage.LastUsage = UsageType.Write;
            return usage;
        }

        public ObjectUsage<T>? GetForRead()
        {
            Stopwatch sw = Stopwatch.StartNew();

            do
            {
                flip(ref backIndex);

                // This should really never happen, but prevents a potential infinite loop if the usage can never be retrieved.
                if (sw.ElapsedMilliseconds > 100)
                    return null;
            } while (buffers[backIndex].LastUsage == UsageType.Read);

            ObjectUsage<T> usage = buffers[backIndex];

            Debug.Assert(usage.LastUsage == UsageType.Write);
            usage.LastUsage = UsageType.Read;

            return usage;
        }

        private void finishUsage(ObjectUsage<T> usage)
        {
            if (usage.LastUsage == UsageType.Write)
                flip(ref frontIndex);
        }

        private void flip(ref int localIndex)
        {
            localIndex = Interlocked.Exchange(ref flipIndex, localIndex);
        }
    }
}
