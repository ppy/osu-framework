// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Allocation
{
    public class ObjectUsage<T> : IDisposable
        where T : class
    {
        public T Object;

        public UsageType Usage;

        public readonly int Index;

        private readonly Action<ObjectUsage<T>> finish;

        public ObjectUsage(int index, Action<ObjectUsage<T>> finish)
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
        None,
        Read,
        Write
    }
}
