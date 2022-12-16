using FlappyDon.Game;
using osu.Framework.Desktop;
using osu.Framework.Platform;

namespace FlappyDon.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = DesktopHost.GetSuitableHost(@"FlappyDon"))
            using (osu.Framework.Game game = new FlappyDonGame())
            {
                host.Run(game);
            }
        }
    }
}
