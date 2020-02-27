using FlappyDon.Game.Elements;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osuTK;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A scene to test the layout and
    /// positioning and rotation of two pipe sprites.
    /// </summary>
    public class TestScenePipeObstacle : TestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new PipeObstacle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1.0f),
            });
        }
    }
}
