using FlappyDon.Game;
using Foundation;
using osu.Framework.iOS;

namespace FlappyDon.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : GameAppDelegate
    {
        protected override osu.Framework.Game CreateGame() => new FlappyDonGame();
    }
}
