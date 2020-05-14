using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene wrapping the entire game,
    /// including audio.
    /// </summary>
    public class TestSceneFlappyDonGame : TestScene
    {
        private FlappyDonGame game;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new FlappyDonGame();
            game.SetHost(host);
            Add(game);
        }
    }
}
