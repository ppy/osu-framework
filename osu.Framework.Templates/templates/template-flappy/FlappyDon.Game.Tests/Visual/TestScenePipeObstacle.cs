using FlappyDon.Game.Elements;
using osu.Framework.Allocation;
using osu.Framework.Graphics;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A scene to test the layout and positioning and rotation of two pipe sprites.
    /// </summary>
    public class TestScenePipeObstacle : FlappyDonTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new PipeObstacle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }
    }
}
