// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Allocation
{
    public class ObjectUsage<T> : IDisposable
        where T : class
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
