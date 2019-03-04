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

            var result = subjectRegion.ClipTo(clipRegion);

            checkVertices(result, subjectRegion.Vertices);
        }

        [Test]
        public void Test1()
        {
            var clipRegion = new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));
            var subjectRegion = new Quad(new Vector2(0.8f, 0.8f), new Vector2(1.8f, 0.8f), new Vector2(0.8f, 0.2f), new Vector2(1.8f, 0.2f));

            var result = subjectRegion.ClipTo(clipRegion);

            checkVertices(result, new Vector2(0.8f, 0.8f), new Vector2(1.0f, 0.8f), new Vector2(1.0f, 0.2f), new Vector2(0.8f, 0.2f));
        }

        private void checkVertices(IConvexPolygon subject, params Vector2[] vertices)
            => Assert.AreEqual(PolygonExtensions.GetRotation(vertices), PolygonExtensions.GetRotation(subject.Vertices), 0.00001f);
    }
}
