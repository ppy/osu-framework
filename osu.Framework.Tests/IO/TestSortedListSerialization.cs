// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Lists;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestSortedListSerialization
    {
        [Test]
        public void TestUnsortedSerialization()
        {
            var original = new SortedList<int>();
            original.AddRange(new[] { 1, 2, 3, 4, 5, 6 });

            var deserialized = JsonConvert.DeserializeObject<SortedList<int>>(JsonConvert.SerializeObject(original));

            Assert.AreEqual(original.Count, deserialized?.Count, "Counts are not equal");
            for (int i = 0; i < original.Count; i++)
                Assert.AreEqual(original[i], deserialized?[i], $"Item at index {i} differs");
        }

        [Test]
        public void TestSortedSerialization()
        {
            var original = new SortedList<int>();
            original.AddRange(new[] { 6, 5, 4, 3, 2, 1 });

            var deserialized = JsonConvert.DeserializeObject<SortedList<int>>(JsonConvert.SerializeObject(original));

            Assert.AreEqual(original.Count, deserialized?.Count, "Counts are not equal");
            for (int i = 0; i < original.Count; i++)
                Assert.AreEqual(original[i], deserialized?[i], $"Item at index {i} differs");
        }

        [Test]
        public void TestEmptySerialization()
        {
            var original = new SortedList<int>();
            var deserialized = JsonConvert.DeserializeObject<SortedList<int>>(JsonConvert.SerializeObject(original));

            Assert.AreEqual(original.Count, deserialized?.Count, "Counts are not equal");
        }

        [Test]
        public void TestCustomComparer()
        {
            static int compare(int i1, int i2) => i2.CompareTo(i1);

            var original = new SortedList<int>(compare);
            original.AddRange(new[] { 1, 2, 3, 4, 5, 6 });

            var deserialized = new SortedList<int>(compare);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(original), deserialized);

            Assert.AreEqual(original.Count, deserialized.Count, "Counts are not equal");
            for (int i = 0; i < original.Count; i++)
                Assert.AreEqual(original[i], deserialized[i], $"Item at index {i} differs");
        }
    }
}
