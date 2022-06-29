using osu.Framework.Allocation;
using osu.Framework.Platform;
using NUnit.Framework;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene wrapping the entire game,
    /// including audio.
    /// </summary>
    [TestFixture]
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
