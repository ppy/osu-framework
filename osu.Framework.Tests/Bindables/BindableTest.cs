// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableTest
    {
        /// <summary>
        /// Tests that a value provided in the constructor is used as the default value for the bindable.
        /// </summary>
        [Test]
        public void TestConstructorValueUsedAsDefaultValue()
        {
            Assert.That(new Bindable<int>(10).Default, Is.EqualTo(10));
        }

        /// <summary>
        /// Tests that a value provided in the constructor is used as the initial value for the bindable.
        /// </summary>
        [Test]
        public void TestConstructorValueUsedAsInitialValue()
        {
            Assert.That(new Bindable<int>(10).Value, Is.EqualTo(10));
        }
    }
}
