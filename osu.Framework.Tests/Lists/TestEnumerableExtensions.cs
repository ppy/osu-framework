// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        }
    }
}
