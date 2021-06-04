using System;
namespace osu.Framework.Platform.MacOS.Native
{
    public class NSSet
    {
        internal IntPtr Handle { get; }

        public NSSet(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
