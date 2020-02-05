using osu.Framework.Graphics;
using osu.Framework.Testing;

namespace TemplateGame.Game.Tests.Visual
{
    public class TestSceneSpinningBox : TestScene
    {
        public TestSceneSpinningBox()
        {
            Add(new SpinningBox
            {
                Anchor = Anchor.Centre,
            });
        }
    }
}
