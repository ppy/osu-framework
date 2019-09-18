// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using NUnit.Framework;
using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class QuadTest
    {
        [TestCaseSource(typeof(AreaTestData), nameof(AreaTestData.TestCases))]
        [DefaultFloatingPointTolerance(0.1f)]
        public void TestArea(Quad testQuad)
        {
            Assert.That(testQuad.ConservativeArea, Is.EqualTo(testQuad.Area).Within(0.1f));
        }

        private class AreaTestData
        {
            public static IEnumerable TestCases
            {
                get
                {
                    // Point
                    yield return new TestCaseData(new Quad(0, 0, 0, 0));

                    // Lines
                    yield return new TestCaseData(new Quad(0, 0, 100, 0));
                    yield return new TestCaseData(new Quad(0, 0, 0, 100));
                    yield return new TestCaseData(new Quad(0, 0, 100, 1));
                    yield return new TestCaseData(new Quad(0, 0, 1, 100));

                    // Simple quads
                    yield return new TestCaseData(new Quad(0, 0, 10, 10));
                    yield return new TestCaseData(new Quad(0, 0, 10, 5));
                    yield return new TestCaseData(new Quad(0, 0, 5, 10));

                    // Rotated simple quads
                    yield return new TestCaseData(new Quad(10, 10, -10, -10));
                    yield return new TestCaseData(new Quad(10, 5, -10, -5));
                    yield return new TestCaseData(new Quad(5, 10, -5, -10));

                    // Sheared quads
                    yield return new TestCaseData(shear(new Quad(0, 0, 10, 10), new Vector2(10)));
                    yield return new TestCaseData(shear(new Quad(0, 0, 10, 10), new Vector2(-10)));

                    // Sheared rotated quads
                    yield return new TestCaseData(shear(new Quad(10, 10, -10, -10), new Vector2(10)));
                    yield return new TestCaseData(shear(new Quad(10, 5, -10, -5), new Vector2(10)));
                    yield return new TestCaseData(shear(new Quad(5, 10, -5, -10), new Vector2(10)));
                }
            }

            private static Quad shear(Quad quad, Vector2 amount)
            {
                var matrix = Matrix3.Identity;
                MatrixExtensions.ShearFromLeft(ref matrix, Vector2.Divide(amount, quad.Size));

                return quad * matrix;
            }
        }
    }
}
