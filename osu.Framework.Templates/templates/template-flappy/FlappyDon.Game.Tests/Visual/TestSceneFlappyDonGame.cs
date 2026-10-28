using osu.Framework.Allocation;
using NUnit.Framework;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene wrapping the entire game,
    /// including audio.
    /// </summary>
    [TestFixture]
    public partial class TestSceneFlappyDonGame : FlappyDonTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            AddGame(new FlappyDonGame());
        }
    }
}
