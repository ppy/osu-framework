// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Lists;

namespace osu.Framework.Tests.Lists
{
    [TestFixture]
    public class TestSortedList
    {
        [Test]
        public void TestAdd()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            Assert.AreEqual(-10, list[0]);
            Assert.AreEqual(8, list[1]);
            Assert.AreEqual(10, list[2]);
            Assert.AreEqual(13, list[3]);
        }

        [Test]
        public void TestRemove()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.Remove(8);
            Assert.IsFalse(list.Any(i => i == 8));
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void TestRemoveAt()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.RemoveAt(0);
            Assert.IsFalse(list.Any(i => i == -10));
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void TestClear()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.Clear();
            Assert.IsFalse(list.Any());
            Assert.AreEqual(0, list.Count);
        }
    }
}
