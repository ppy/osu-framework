using OpenTK;
using System;

namespace osu.Framework.Graphics.Containers
{
    public class PaddingContainer : Container
    {
        internal override Vector2 ChildSize => base.ChildSize - new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

        internal override Vector2 ChildOffset => base.ChildOffset + new Vector2(Padding.Left, Padding.Top);

        private Padding padding;
        public Padding Padding
        {
            get { return padding; }
            set
            {
                if (padding.Equals(value)) return;

                padding = value;

                Invalidate(Invalidation.Position);
            }
        }
    }

    public struct Padding : IEquatable<Padding>
    {
        public float Top;
        public float Left;
        public float Bottom;
        public float Right;

        public Padding(float allSides)
        {
            Top = Left = Bottom = Right = allSides;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public bool Equals(Padding other)
        {
            return Top == other.Top && Left == other.Left && Bottom == other.Bottom && Right == other.Right;
        }
    }
}
