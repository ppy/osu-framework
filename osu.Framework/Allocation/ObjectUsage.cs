using System;

namespace osu.Framework.Allocation
{
    public class ObjectUsage<U> : IDisposable
    {
        public U Object;

        internal Action<ObjectUsage<U>, UsageType> Finish;

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
