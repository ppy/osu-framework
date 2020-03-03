using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// A full-screen white flash used to indicate a collision with a pipe, or when the game scene resets after a game over.
    /// </summary>
    public class ScreenFlash : Box
    {
        /// <summary>
        /// The duration for the screen flash to play.
        /// </summary>
        public float FlashDuration = 30.0f;

        public ScreenFlash()
        {
            Colour = Color4.White;
            RelativeSizeAxes = Axes.Both;
            Alpha = 0.0f;
        }

        public void GameOverFlash()
        {
            this.FadeIn(FlashDuration).Then().FadeOut(500.0f);
        }

        public void ResetFlash()
        {
            this.FadeIn().Then().FadeOut(700.0f);
        }
    }
}
