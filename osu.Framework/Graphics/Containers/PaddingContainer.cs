using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class PaddingContainer : Container
    {
        public override Vector2 Size => base.Size - new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

        protected override Vector2 GetAnchoredPosition(Vector2 pos) => base.GetAnchoredPosition(pos) + new Vector2(Padding.Left, Padding.Top);

        public Padding Padding;
    }

    public struct Padding
    {
        public float Top;
        public float Left;
        public float Bottom;
        public float Right;
    }
}
