// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableStringTest
    {
        [TestCase("")]
        [TestCase(null)]
        [TestCase("this is a string")]
        public void TestSet(string value)
        {
            var bindable = new Bindable<string> { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("")]
        [TestCase("null")]
        [TestCase("this is a string")]
        public void TestParsingString(string value)
        {
            var bindable = new Bindable<string>();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
