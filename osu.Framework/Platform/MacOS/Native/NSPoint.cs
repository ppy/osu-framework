

namespace osu.Framework.Platform.MacOS.Native
{
    public struct NSPoint
    {
        public float X;
        public float Y;

        public NSPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct NSPoint_64
    {
        public double X;
        public double Y;

        public NSPoint ToNSPoint() => new NSPoint((float)X, (float)Y);
    }
}
