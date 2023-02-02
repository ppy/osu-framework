using osu.Framework.Platform;
using osu.Framework;
using TemplateGame.Game;

namespace TemplateGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"TemplateGame"))
                host.Run(new TemplateGameGame());
        }
    }
}
