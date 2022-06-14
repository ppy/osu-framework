// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Tests.Lists
{
    [TestFixture]
    public class TestEnumerableExtensions
    {
        [Test]
        public void TestCommonPrefix()
        {
            string[] arr = { "abcd", "abdc" };
            Assert.AreEqual("ab", arr.GetCommonPrefix());

            arr = new[] { "123.456", "123.789" };
            Assert.AreEqual("123.", arr.GetCommonPrefix());

            arr = new[] { "123.456", "123.789", "435", "123.789" };
            Assert.AreEqual(string.Empty, arr.GetCommonPrefix());
        }

        [Test]
        public void TestEmptyEnumerable()
        {
            Assert.AreEqual(string.Empty, Enumerable.Empty<string>().GetCommonPrefix());
        }

        [TestCase("a", null)]
        [TestCase("b", "a")]
        [TestCase("c", "b")]
        [TestCase("d", null)]
        public void TestGetPrevious(string item, string expected)
        {
            string[] arr = { "a", "b", "c" };
            Assert.That(arr.GetPrevious(item), Is.EqualTo(expected));
        }

        [TestCase("a", "b")]
        [TestCase("b", "c")]
        [TestCase("c", null)]
        [TestCase("d", null)]
        public void TestGetNext(string item, string expected)
        {
            string[] arr = { "a", "b", "c" };
            Assert.That(arr.GetNext(item), Is.EqualTo(expected));
        }
    }
}
