using FlappyDon.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;

namespace FlappyDon.Game
{
    /// <summary>
    /// Set up the relevant resource stores and texture settings.
    /// </summary>
    public abstract class FlappyDonGameBase : osu.Framework.Game
    {
        protected override TextureFilteringMode DefaultTextureFilteringMode
            // To preserve the 8-bit aesthetic, disable texture filtering
            // so they won't become blurry when upscaled
            => TextureFilteringMode.Nearest;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load the assets from our Resources project
            Resources.AddStore(new DllResourceStore(FlappyDonResources.ResourceAssembly));
        }
    }
}
