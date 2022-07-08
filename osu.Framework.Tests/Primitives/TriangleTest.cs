// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osuTK;
using System.Collections;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class TriangleTest
    {
        [TestCaseSource(typeof(AreaTestData), nameof(AreaTestData.TestCases))]
        [DefaultFloatingPointTolerance(0.1f)]
        public float TestArea(Triangle testTriangle) => testTriangle.Area;

        private class AreaTestData
        {
            public static IEnumerable TestCases
            {
                get
                {
                    // Point
                    yield return new TestCaseData(new Triangle(Vector2.Zero, Vector2.Zero, Vector2.Zero)).Returns(0);

                    // Angled
                    yield return new TestCaseData(new Triangle(Vector2.Zero, new Vector2(10, 0), new Vector2(10))).Returns(50);
                    yield return new TestCaseData(new Triangle(Vector2.Zero, new Vector2(0, 10), new Vector2(10))).Returns(50);
                    yield return new TestCaseData(new Triangle(Vector2.Zero, new Vector2(10, -10), new Vector2(10))).Returns(100);
                    yield return new TestCaseData(new Triangle(Vector2.Zero, new Vector2(10, -10), new Vector2(10))).Returns(100);
                }
            }
        }
    }
}
