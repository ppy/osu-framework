using FlappyDon.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK.Graphics.ES30;

namespace FlappyDon.Game
{
    /// <summary>
    /// Set up the relevant resource stores and texture settings.
    /// </summary>
    public abstract class FlappyDonGameBase : osu.Framework.Game
    {
        private TextureStore textures;

        private DependencyContainer dependencies;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            // Load the assets from our Resources project
            Resources.AddStore(new DllResourceStore(FlappyDonResources.ResourceAssembly));

            // To preserve the 8-bit aesthetic, disable texture filtering
            // so they won't become blurry when upscaled
            textures = new TextureStore(Textures, filteringMode: All.Nearest);
            dependencies.Cache(textures);
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
    }
}
