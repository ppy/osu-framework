using osu.Framework.Platform;
using osu.Framework.Desktop;
using TemplateGame.Game;

namespace TemplateGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = DesktopHost.GetSuitableHost(@"TemplateGame"))
            using (osu.Framework.Game game = new TemplateGameGame())
                host.Run(game);
        }
    }
}
