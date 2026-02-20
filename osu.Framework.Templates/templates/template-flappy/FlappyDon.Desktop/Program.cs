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
                host.Run(new FlappyDonGame());
        }
    }
}
