using FlappyDon.Game;
using osu.Framework.iOS;

namespace FlappyDon.iOS
{
    /// <inheritdoc />
    public class AppDelegate : GameApplicationDelegate
    {
        /// <inheritdoc />
        protected override osu.Framework.Game CreateGame() => new FlappyDonGame();
    }
}
