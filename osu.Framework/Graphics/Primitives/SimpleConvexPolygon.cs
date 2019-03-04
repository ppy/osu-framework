using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    public struct SimpleConvexPolygon : IConvexPolygon
    {
        public Vector2[] Vertices => vertices;

        public Vector2[] AxisVertices => vertices;

        private readonly Vector2[] vertices;

        public SimpleConvexPolygon(Vector2[] vertices)
        {
            this.vertices = vertices;
        }

        public bool Contains(Vector2 v) => false;
    }
}
