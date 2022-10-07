using FlappyDon.Game.Elements;
using osu.Framework.Allocation;
using NUnit.Framework;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene for testing the alignment
    /// and placement of the sprites that make up the backdrop
    /// </summary>
    [TestFixture]
    public class TestSceneBackdrop : FlappyDonTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Backdrop(() => new BackdropSprite()));
        }
    }
}
