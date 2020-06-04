using FlappyDon.Game.Elements;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A scene to test the layout and positioning and rotation of two pipe sprites.
    /// </summary>
    public class TestSceneObstacles : TestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Obstacles obstacles = new Obstacles
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
            obstacles.Start();

            Add(obstacles);
        }
    }
}
