using FlappyDon.Game;
using osu.Framework;
using osu.Framework.Platform;

namespace FlappyDon.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"FlappyDon"))
            using (osu.Framework.Game game = new FlappyDonGame())
            {
                host.Run(game);
            }
        }
    }
}
