namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation that switches the displayed drawable when a new frame is displayed.
    /// </summary>
    public class DrawableAnimation : Animation<Drawable>
    {
        protected override void DisplayFrame(Drawable content)
        {
            Clear(false);
            Add(content);
        }
    }
}
