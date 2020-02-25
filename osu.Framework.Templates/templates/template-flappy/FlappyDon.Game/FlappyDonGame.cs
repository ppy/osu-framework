// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FlappyDon.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK.Graphics.ES30;

namespace FlappyDon.Game
{
    /// <summary>
    /// The main entry point to the game.
    /// Sets up the relevant resource stores
    /// and texture settings.
    /// </summary>
    public class FlappyDonGame : osu.Framework.Game
    {
        private TextureStore textures;
        private GameScreen gameScreen;

        private DependencyContainer dependencies;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            // Load the assets from our Resources project
            Resources.AddStore(new DllResourceStore(typeof(FlappyDonResources).Assembly));

            // To preserve the 8-bit aesthetic, disable texture filtering
            // so they won't become blurry when upscaled
            textures = new TextureStore(Textures, filteringMode: All.Nearest);
            dependencies.Cache(textures);

            // Add the main screen to this container
            Add(gameScreen = new GameScreen());
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
    }
}
