using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace TemplateGame.Game
{
    public class MainScreen : Screen
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new SpinningBox
            {
                Anchor = Anchor.Centre,
            });
        }
    }
}
