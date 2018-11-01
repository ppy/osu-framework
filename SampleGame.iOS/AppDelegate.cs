using Foundation;
using osu.Framework;
using osu.Framework.iOS;

namespace SampleGame.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : GameAppDelegate
    {
        protected override Game CreateGame() => new SampleGameGame();
    }
}
