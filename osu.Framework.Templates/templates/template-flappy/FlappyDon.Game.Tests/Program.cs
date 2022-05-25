using FlappyDon.Game.Testing;
using osu.Framework;
using osu.Framework.Platform;

namespace FlappyDon.Game.Tests
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost("visual-tests"))
            using (var game = new FlappyDonTestBrowser())
                host.Run(game);
        }
    }
}
