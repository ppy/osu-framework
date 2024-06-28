// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Allocation
{
    public class ObjectUsage<T> : IDisposable
        where T : class
    {
        public T? Object;

        public volatile UsageType LastUsage;

        public readonly int Index;

        private readonly Action<ObjectUsage<T>>? finish;

        public ObjectUsage(int index, Action<ObjectUsage<T>>? finish)
        {
            Index = index;
            this.finish = finish;
        }

        public void Dispose()
        {
            finish?.Invoke(this);
        }
    }

    public enum UsageType
    {
        Read,
        Write
    }
}
