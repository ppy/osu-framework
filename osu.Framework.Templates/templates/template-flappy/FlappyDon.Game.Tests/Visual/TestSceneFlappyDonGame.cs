using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene wrapping the entire game,
    /// including audio.
    /// </summary>
    public class TestSceneFlappyDonGame : FlappyDonTestScene
    {
        private FlappyDonGame game;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new FlappyDonGame();
            game.SetHost(host);
            AddGame(game);
        }
    }
}
