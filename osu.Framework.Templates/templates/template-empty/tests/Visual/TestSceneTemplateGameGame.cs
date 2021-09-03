using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace Template.Game.Tests.Visual
{
    public class TestSceneApplication : ApplicationTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private Application game;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new Application();
            game.SetHost(host);

            Add(game);
        }
    }
}
