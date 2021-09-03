using osu.Framework;
using osu.Framework.Platform;

namespace Template.Game.Tests
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost("visual-tests"))
            using (var game = new ApplicationTestBrowser())
                host.Run(game);
        }
    }
}
