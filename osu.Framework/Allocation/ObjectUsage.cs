// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Allocation
{
    public class ObjectUsage<T> : IDisposable
    {
        public T Object;
        public int Index;

        public long FrameId;

        internal Action<ObjectUsage<T>, UsageType> Finish;

        public UsageType Usage;

        public void Dispose()
        {
            Finish?.Invoke(this, Usage);
        }
    }

    public enum UsageType
    {
        None,
        Read,
        Write
    }
}
