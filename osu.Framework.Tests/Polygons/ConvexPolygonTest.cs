using System;
using NUnit.Framework;
using osu.Framework.Extensions.PolygonExtensions;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class ConvexPolygonTest
    {
        [Test]
        public void TestClipFullyContainedSubject()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, subjectRegion.Vertices);
        }

        [Test]
        public void TestClipFullyContainedClip()
        {
            var clipRegion = new Quad(new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.8f), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f));
            var subjectRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, clipRegion.Vertices);
        }

        [Test]
        public void TestClipSelf()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));

            var result = clip(clipRegion, clipRegion);

            checkVertices(result, clipRegion.Vertices);
        }

        [Test]
        public void TestClipColinearExternal()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(1, 0.8f), new Vector2(2, 0.8f), new Vector2(1, 0.2f), new Vector2(2, 0.2f));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, new Vector2(1, 0.8f), new Vector2(1, 0.8f), new Vector2(1, 0.2f), new Vector2(1, 0.2f));
        }

        [Test]
        public void TestClipColinearInternal()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(0.5f, 0.8f), new Vector2(1, 0.8f), new Vector2(0.5f, 0.2f), new Vector2(1, 0.2f));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, new Vector2(0.5f, 0.8f), new Vector2(1, 0.8f), new Vector2(1, 0.2f), new Vector2(0.5f, 0.2f));
        }


        [Test]
        public void TestSingleSideIntersection()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(0.8f, 0.8f), new Vector2(1.8f, 0.8f), new Vector2(0.8f, 0.2f), new Vector2(1.8f, 0.2f));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, new Vector2(0.8f, 0.8f), new Vector2(1.0f, 0.8f), new Vector2(1.0f, 0.2f), new Vector2(0.8f, 0.2f));
        }

        [Test]
        public void TestMultipleSideIntersection()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f), new Vector2(0.5f, 0.5f), new Vector2(1.5f, 0.5f));

            var result = clip(subjectRegion, clipRegion);

            checkVertices(result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), Vector2.One, new Vector2(1, 0.5f));
        }

        private IConvexPolygon clip(IConvexPolygon subject, IConvexPolygon clip)
        {
            var clipper = new ConvexPolygonClipper(subject, clip);

            Span<Vector2> buffer = stackalloc Vector2[clipper.GetBufferSize()];
            Span<Vector2> clippedRegion = clipper.Clip(buffer);

            return new SimpleConvexPolygon(clippedRegion.ToArray());
        }

        private void checkVertices(IConvexPolygon subject, params Vector2[] vertices)
            => Assert.AreEqual(new ConvexPolygonClipper().GetRotation(vertices), new ConvexPolygonClipper().GetRotation(subject.Vertices), 0.00001f);
    }
}
