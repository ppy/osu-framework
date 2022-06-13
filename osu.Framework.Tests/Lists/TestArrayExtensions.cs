// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Lists
{
    [TestFixture]
    public class TestArrayExtensions
    {
        [Test]
        public void TestNullToJagged()
        {
            int[][] result = null;
            Assert.DoesNotThrow(() => result = ((int[,])null).ToJagged());
            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestNullToRectangular()
        {
            int[,] result = null;
            Assert.DoesNotThrow(() => result = ((int[][])null).ToRectangular());
            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestEmptyRectangularToJagged()
        {
            int[][] result = null;
            Assert.DoesNotThrow(() => result = new int[0, 0].ToJagged());
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void TestEmptyJaggedToRectangular()
        {
            int[,] result = null;
            Assert.DoesNotThrow(() => result = Array.Empty<int[]>().ToRectangular());
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void TestRectangularColumnToJagged()
        {
            int[][] result = null;
            Assert.DoesNotThrow(() => result = new int[1, 10].ToJagged());
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(10, result[0].Length);
        }

        [Test]
        public void TestJaggedColumnToRectangular()
        {
            int[][] jagged = new int[10][];

            int[,] result = null;
            Assert.DoesNotThrow(() => result = jagged.ToRectangular());
            Assert.AreEqual(10, result.GetLength(0));
        }

        [Test]
        public void TestRectangularRowToJagged()
        {
            int[][] result = null;
            Assert.DoesNotThrow(() => result = new int[10, 0].ToJagged());
            Assert.AreEqual(10, result.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(0, result[i].Length);
        }

        [Test]
        public void TestJaggedRowToRectangular()
        {
            int[][] jagged = new int[1][];
            jagged[0] = new int[10];

            int[,] result = null;
            Assert.DoesNotThrow(() => result = jagged.ToRectangular());
            Assert.AreEqual(10, result.GetLength(1));
            Assert.AreEqual(1, result.GetLength(0));
        }

        [Test]
        public void TestSquareRectangularToJagged()
        {
            int[][] result = null;
            Assert.DoesNotThrow(() => result = new int[10, 10].ToJagged());
            Assert.AreEqual(10, result.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(10, result[i].Length);
        }

        [Test]
        public void TestSquareJaggedToRectangular()
        {
            int[][] jagged = new int[10][];
            for (int i = 0; i < 10; i++)
                jagged[i] = new int[10];

            int[,] result = null;
            Assert.DoesNotThrow(() => result = jagged.ToRectangular());
            Assert.AreEqual(10, result.GetLength(0));
            Assert.AreEqual(10, result.GetLength(1));
        }

        [Test]
        public void TestNonSquareJaggedToRectangular()
        {
            int[][] jagged = new int[10][];
            for (int i = 0; i < 10; i++)
                jagged[i] = new int[i];

            int[,] result = null;
            Assert.DoesNotThrow(() => result = jagged.ToRectangular());
            Assert.AreEqual(10, result.GetLength(0));
            Assert.AreEqual(9, result.GetLength(1));
        }

        [Test]
        public void TestNonSquareJaggedWithNullRowsToRectangular()
        {
            int[][] jagged = new int[10][];

            for (int i = 1; i < 10; i += 2)
            {
                if (i % 2 == 1)
                    jagged[i] = new int[i];
            }

            int[,] result = null;
            Assert.DoesNotThrow(() => result = jagged.ToRectangular());
            Assert.AreEqual(10, result.GetLength(0));
            Assert.AreEqual(9, result.GetLength(1));

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(0, result[i, 0]);
        }

        [Test]
        public void TestInvertRectangular()
        {
            int?[,] original =
            {
                { 1, 2, null },
                { null, 3, 4 },
                { 5, 6, null }
            };

            int?[,] result = original.Invert();

            Assert.AreEqual(1, result[0, 0]);
            Assert.AreEqual(2, result[1, 0]);
            Assert.AreEqual(null, result[2, 0]);
            Assert.AreEqual(null, result[0, 1]);
            Assert.AreEqual(3, result[1, 1]);
            Assert.AreEqual(4, result[2, 1]);
            Assert.AreEqual(5, result[0, 2]);
            Assert.AreEqual(6, result[1, 2]);
            Assert.AreEqual(null, result[2, 2]);
        }

        [Test]
        public void TestInvertJagged()
        {
            // 4x5 array
            int?[][] original =
            {
                new int?[] { 1, 2, null },
                new int?[] { 3, 4 },
                null,
                new int?[] { null, 5, 6, 7, 8 }
            };

            int?[][] result = original.Invert();

            // Ensure 5x4 array
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual(4, result.Max(r => r.Length));

            // Column 1
            Assert.AreEqual(1, result[0][0]);
            Assert.AreEqual(2, result[1][0]);
            Assert.AreEqual(null, result[2][0]);
            Assert.AreEqual(null, result[3][0]);
            Assert.AreEqual(null, result[4][0]);

            // Column 2
            Assert.AreEqual(3, result[0][1]);
            Assert.AreEqual(4, result[1][1]);
            Assert.AreEqual(null, result[2][1]);
            Assert.AreEqual(null, result[3][1]);
            Assert.AreEqual(null, result[4][1]);

            // Column 3
            Assert.AreEqual(null, result[0][2]);
            Assert.AreEqual(null, result[1][2]);
            Assert.AreEqual(null, result[2][2]);
            Assert.AreEqual(null, result[3][2]);
            Assert.AreEqual(null, result[4][2]);

            // Column 4
            Assert.AreEqual(null, result[0][3]);
            Assert.AreEqual(5, result[1][3]);
            Assert.AreEqual(6, result[2][3]);
            Assert.AreEqual(7, result[3][3]);
            Assert.AreEqual(8, result[4][3]);
        }
    }
}
