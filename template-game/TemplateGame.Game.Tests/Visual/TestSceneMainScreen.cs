using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Framework.Testing;

namespace TemplateGame.Game.Tests.Visual
{
    public class TestSceneMainScreen : TestScene
    {
        public TestSceneMainScreen()
        {
            Add(new ScreenStack(new MainScreen()) { RelativeSizeAxes = Axes.Both });
        }
    }
}
