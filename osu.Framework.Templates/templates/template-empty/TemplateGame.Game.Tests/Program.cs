using osu.Framework;
using osu.Framework.Platform;

namespace TemplateGame.Game.Tests
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost("visual-tests"))
                host.Run(new TemplateGameTestBrowser());
        }
    }
}
