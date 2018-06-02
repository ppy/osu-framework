// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Configuration;
using System.Drawing;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableSizeTest
    {
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(int.MinValue, int.MinValue)]
        [TestCase(int.MaxValue, int.MaxValue)]
        [TestCase(int.MinValue, 0)]
        public void TestSet(int width, int height)
        {
            var value = new Size(width, height);
            var bindable = new BindableSize { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("0x0", 0, 0)]
        [TestCase("1920x1080", 1920, 1080)]
        [TestCase("-1000x0", -1000, 0)]
        [TestCase("0x-1000", 0, -1000)]
        public void TestParsingString(string value, int expectedWidth, int expectedHeight)
        {
            var bindable = new BindableSize();
            bindable.Parse(value);

            Assert.AreEqual(new Size(expectedWidth, expectedHeight), bindable.Value);
        }
    }
}
