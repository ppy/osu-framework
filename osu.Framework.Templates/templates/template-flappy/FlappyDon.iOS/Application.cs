using FlappyDon.Game;
using osu.Framework.iOS;

namespace FlappyDon.iOS
{
    public static class Application
    {
        public static void Main(string[] args) => GameApplication.Main(new FlappyDonGame());
    }
}
