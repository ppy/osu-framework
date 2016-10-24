// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Threading;

namespace osu.Framework.Threading
{
    public class AtomicCounter
    {
        private long count;

        public void Increment()
        {
            Interlocked.Increment(ref count);
        }

        public void Add(long value)
        {
            Interlocked.Add(ref count, value);
        }

        public long Reset()
        {
            return Interlocked.Exchange(ref count, 0);
        }

        public long Value
        {
            set
            {
                Interlocked.Exchange(ref count, value);
            }

            get
            {
                return Interlocked.Read(ref count);
            }
        }
    }
}
