// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
            Assert.DoesNotThrow(() => result = new int[0][].ToRectangular());
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
            var jagged = new int[10][];

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
            var jagged = new int[1][];
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
            var jagged = new int[10][];
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
            var jagged = new int[10][];
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
            var jagged = new int[10][];
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
    }
}
