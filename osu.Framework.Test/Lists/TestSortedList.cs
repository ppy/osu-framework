using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Lists;
using Xunit;

namespace osu.Framework.Test.Lists
{
    public class TestSortedList
    {
        [Fact]
        public void TestAdd()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            Assert.Collection(list,
                i => Assert.Equal(-10, i),
                i => Assert.Equal(8, i),
                i => Assert.Equal(10, i),
                i => Assert.Equal(13, i));
        }
        
        [Fact]
        public void TestRemove()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.Remove(8);
            Assert.False(list.Any(i => i == 8));
            Assert.Equal(3, list.Count);
        }
        
        [Fact]
        public void TestRemoveAt()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.RemoveAt(0);
            Assert.False(list.Any(i => i == -10));
            Assert.Equal(3, list.Count);
        }
        
        [Fact]
        public void TestClear()
        {
            var list = new SortedList<int>(Comparer<int>.Create((a, b) => a - b));
            list.Add(10);
            list.Add(8);
            list.Add(13);
            list.Add(-10);
            list.Clear();
            Assert.False(list.Any());
            Assert.Equal(0, list.Count);
        }
    }
}