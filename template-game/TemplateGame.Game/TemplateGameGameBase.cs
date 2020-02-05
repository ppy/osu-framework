using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace TemplateGame.Game
{
    public class TemplateGameGameBase : osu.Framework.Game
    {
        // Anything in this class is shared between the test browser and the game implementation.
        // It allows for caching global dependencies that should be accessible to tests, or changing
        // the screen scaling for all components including the test browser and framework overlays.

        protected override Container<Drawable> Content { get; }

        protected TemplateGameGameBase()
        {
            // ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer());
        }
    }
}
