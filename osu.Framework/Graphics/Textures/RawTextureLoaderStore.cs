using osu.Framework.Graphics.Textures.Png;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureLoaderStore : ResourceStore<RawTexture>
    {
        private IResourceStore<byte[]> Store { get; set; }
    
        public RawTextureLoaderStore(IResourceStore<byte[]> store)
        {
            Store = store;
        }

        public override RawTexture Get(string name)
        {
            RawTexture t = new RawTexture();
            using (var stream = Store.GetStream(name))
            {
                var reader = new PngReader();
                t.Pixels = reader.Read(stream);
                t.PixelFormat = OpenTK.Graphics.ES20.PixelFormat.Rgba;
                t.Width = reader.Width;
                t.Height = reader.Height;
            }
            return t;
        }
    }
}