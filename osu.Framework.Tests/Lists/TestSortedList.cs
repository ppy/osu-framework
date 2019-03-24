// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Lists;
using NUnit.Framework;

namespace osu.Framework.Tests.Lists
{
    [TestFixture]
    public class TestSortedList
    {
        [Test]
        public void TestAdd()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b))
            {
                10,
                8,
                13,
                -10
            };
            Assert.AreEqual(-10, list[0]);
            Assert.AreEqual(8, list[1]);
            Assert.AreEqual(10, list[2]);
            Assert.AreEqual(13, list[3]);
        }

        [Test]
        public void TestRemove()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b))
            {
                10,
                8,
                13,
                -10
            };
            list.Remove(8);
            Assert.That(list, Does.Not.Contain(8));
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void TestRemoveAt()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b))
            {
                10,
                8,
                13,
                -10
            };
            list.RemoveAt(0);
            Assert.That(list, Does.Not.Contain(-10));
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void TestClear()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b))
            {
                10,
                8,
                13,
                -10
            };
            list.Clear();
            Assert.That(list, Is.Empty);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void TestIndexOf()
        {
            var list = new SortedList<int>(Comparer<int>.Default)
            {
                10,
                10,
                10,
            };

            Assert.IsTrue(list.IndexOf(10) >= 0);
        }
    }
}
