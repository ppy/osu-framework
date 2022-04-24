using FlappyDon.Game.Elements;
using osu.Framework.Allocation;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene for testing the alignment
    /// and placement of the sprites that make up the backdrop
    /// </summary>
    public class TestSceneBackdrop : FlappyDonTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Backdrop(() => new BackdropSprite()));
        }
    }
}
