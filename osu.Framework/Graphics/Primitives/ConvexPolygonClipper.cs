using System;
using System.Linq;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    public readonly ref struct ConvexPolygonClipper
    {
        private readonly IConvexPolygon subjectPolygon;
        private readonly IConvexPolygon clipPolygon;

        public ConvexPolygonClipper(IConvexPolygon subjectPolygon, IConvexPolygon clipPolygon)
        {
            this.subjectPolygon = subjectPolygon;
            this.clipPolygon = clipPolygon;
        }

        public int GetBufferSize() => subjectPolygon.GetVertices().Length * 2; // Every edge can contribute at most 2 vertices to the intersection

        public Span<Vector2> Clip(Span<Vector2> buffer)
        {
            ReadOnlySpan<Vector2> subjectVertices = subjectPolygon.GetVertices();

            Span<Vector2> outputVertices = buffer;
            subjectVertices.CopyTo(outputVertices);

            Span<Line> clipEdges = stackalloc Line[clipPolygon.GetVertices().Length];
            fillEdges(clipPolygon, clipEdges);

            int inputCount = subjectVertices.Length;
            Span<Vector2> inputVertices = stackalloc Vector2[outputVertices.Length];

            foreach (var ce in clipEdges)
            {
                if (inputCount == 0)
                    break;

                outputVertices.CopyTo(inputVertices);

                int outputCount = 0;

                var startPoint = inputVertices[inputCount - 1];
                for (int i = 0; i < inputCount; i++)
                {
                    var endPoint = inputVertices[i];
                    if (ce.IsInside(endPoint))
                    {
                        if (!ce.IsInside(startPoint))
                            outputVertices[outputCount++] = ce.At(ce.Intersect(new Line(startPoint, endPoint)).distance);
                        outputVertices[outputCount++] = endPoint;
                    }
                    else if (ce.IsInside(startPoint))
                        outputVertices[outputCount++] = ce.At(ce.Intersect(new Line(startPoint, endPoint)).distance);
                    startPoint = endPoint;
                }

                inputCount = outputCount;
            }

            return outputVertices.Slice(0, inputCount);
        }

        private void fillEdges(IConvexPolygon polygon, Span<Line> edges)
        {
            var vertices = polygon.GetVertices();

            if (GetRotation(vertices) < 0)
            {
                for (int i = vertices.Length - 1, c = 0; i > 0; i--, c++)
                    edges[c] = new Line(vertices[i], vertices[i - 1]);
                edges[edges.Length - 1] = new Line(vertices[0], vertices[vertices.Length - 1]);
            }
            else
            {
                for (int i = 0; i < vertices.Length - 1; i++)
                    edges[i] = new Line(vertices[i], vertices[i + 1]);
                edges[edges.Length - 1] = new Line(vertices[vertices.Length - 1], vertices[0]);
            }
        }

        public float GetRotation(ReadOnlySpan<Vector2> vertices)
        {
            float rotation = 0;
            for (int i = 0; i < vertices.Length - 1; ++i)
            {
                var vi = vertices[i];
                var vj = vertices[i + 1];

                rotation += (vj.X - vi.X) * (vj.Y + vi.Y);
            }

            rotation += (vertices[0].X - vertices[vertices.Length - 1].X) * (vertices[0].Y + vertices[vertices.Length - 1].Y);

            return rotation;
        }

        private void clockwiseSort(Span<Vector2> vertices)
        {
            if (GetRotation(vertices) < 0)
                vertices.Reverse();
        }
    }
}
