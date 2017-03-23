using osu.Framework.Graphics;

namespace osu.Framework.Input
{
    /// <summary>
    /// Declares that a drawable is requesting mouse position updates (via OnMouseMove) as frequently as possible.
    /// </summary>
    public interface IRequireAccurateMousePosition : IDrawable
    {
    }
}
