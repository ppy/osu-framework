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
        public ScreenFlash()
        {
            Colour = Color4.White;
            RelativeSizeAxes = Axes.Both;
            Alpha = 0.0f;
        }

        public void Flash(double fadeInDuration, double fadeOutDuration)
        {
            this.FadeIn(fadeInDuration).Then().FadeOut(fadeOutDuration);
        }
    }
}
