// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableBoolTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public void TestSet(bool value)
        {
            var bindable = new BindableBool { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("True", true)]
        [TestCase("true", true)]
        [TestCase("False", false)]
        [TestCase("false", false)]
        [TestCase("1", true)]
        [TestCase("0", false)]
        public void TestParsingString(string value, bool expected)
        {
            var bindable = new BindableBool();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestParsingBoolean(bool value)
        {
            var bindable = new BindableBool();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
