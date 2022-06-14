// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class LineIntersections
    {
        private static readonly Vector2 origin = Vector2.Zero;
        private static readonly Vector2 up_1 = new Vector2(0, 1);
        private static readonly Vector2 up_2 = new Vector2(0, 2);
        private static readonly Vector2 down_1 = new Vector2(0, -1);
        private static readonly Vector2 left_1 = new Vector2(-1, 0);
        private static readonly Vector2 left_2 = new Vector2(-2, 0);
        private static readonly Vector2 right_1 = new Vector2(1, 0);
        private static readonly Vector2 right_2 = new Vector2(2, 0);

        private static readonly object[] test_cases =
        {
            // Parallel
            new object[] { new Line(origin, up_1), new Line(right_1, right_1 + up_1), false, 0f },
            new object[] { new Line(up_1, origin), new Line(right_1 + up_1, right_1), false, 0f },
            // Parallel diagonal
            new object[] { new Line(origin, right_1 + up_1), new Line(down_1, right_1), false, 0f },
            new object[] { new Line(right_1 + up_1, origin), new Line(down_1, right_1), false, 0f },
            // Touching at endpoints of l2
            new object[] { new Line(origin, up_2), new Line(up_1, up_1 + right_1), true, 0.5f },
            new object[] { new Line(up_2, origin), new Line(up_1 + right_1, up_1), true, 0.5f },
            // Touching at endpoints of l1
            new object[] { new Line(origin, up_1), new Line(left_1 + up_1, up_1 + right_1), true, 1f },
            new object[] { new Line(up_1, origin), new Line(up_1 + right_1, left_1 + up_1), true, 0f },
            // Touching at both endpoints
            new object[] { new Line(origin, up_1), new Line(up_1, up_1 + right_1), true, 1f },
            new object[] { new Line(up_1, origin), new Line(up_1, up_1 + right_1), true, 0f },
            new object[] { new Line(origin, up_1), new Line(up_1 + right_1, up_1), true, 1f },
            new object[] { new Line(up_1, origin), new Line(up_1 + right_1, up_1), true, 0f },
            // Crossing lines
            new object[] { new Line(down_1, up_1), new Line(left_1, right_1), true, 0.5f },
            new object[] { new Line(up_1, down_1), new Line(right_1, left_1), true, 0.5f },
            // External to l1
            new object[] { new Line(origin, up_2), new Line(up_1 + right_1, up_1 + right_2), true, 0.5f },
            new object[] { new Line(up_2, origin), new Line(up_1 + right_1, up_1 + right_2), true, 0.5f },
            new object[] { new Line(origin, up_2), new Line(up_1 + left_2, up_1 + left_1), true, 0.5f },
            new object[] { new Line(up_2, origin), new Line(up_1 + left_2, up_1 + left_1), true, 0.5f },
            // External to l1, out of bounds
            new object[] { new Line(origin, up_1), new Line(up_2 + right_1, up_2 + right_2), true, 2f },
            new object[] { new Line(origin, up_1), new Line(up_2 + left_2, up_2 + left_1), true, 2f },
            new object[] { new Line(up_1, origin), new Line(up_2 + right_1, up_2 + right_2), true, -1f },
            new object[] { new Line(up_1, origin), new Line(up_2 + left_2, up_2 + left_1), true, -1f },
            // Overlapping lines
            new object[] { new Line(origin, up_1), new Line(origin, up_1), false, 0f },
            new object[] { new Line(up_1, origin), new Line(origin, up_1), false, 0f },
            new object[] { new Line(origin, up_1), new Line(up_1, origin), false, 0f },
            new object[] { new Line(up_1, origin), new Line(up_1, origin), false, 0f },
            // Collinear touching
            new object[] { new Line(origin, up_1), new Line(origin, down_1), false, 0f },
            new object[] { new Line(origin, up_1), new Line(down_1, origin), false, 0f },
        };

        [TestCaseSource(nameof(test_cases))]
        public void TestIntersections(Line l1, Line l2, bool expectedResult, float expectedT)
        {
            (bool success, float t) = l1.IntersectWith(l2);

            Assert.That(success, Is.EqualTo(expectedResult));
            Assert.That(t, Is.EqualTo(expectedT));
        }

        [Test]
        public void TestCollinearPointNotInRightHalfPlane()
        {
            Line line = new Line(new Vector2(-0.5f, 0.1f), new Vector2(-10, 2));
            Assert.That(new Vector2(0.5f, -0.1f).InRightHalfPlaneOf(line), Is.False);
        }
    }
}
