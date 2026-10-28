using osu.Framework.iOS;
using TemplateGame.Game;

namespace TemplateGame.iOS
{
    /// <inheritdoc />
    public class AppDelegate : GameApplicationDelegate
    {
        /// <inheritdoc />
        protected override osu.Framework.Game CreateGame() => new TemplateGameGame();
    }
}
