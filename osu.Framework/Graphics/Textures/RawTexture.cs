using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics.Textures
{
    public class RawTexture
    {
        public int Width, Height;
        public PixelFormat PixelFormat;
        public byte[] Pixels;
    }
}