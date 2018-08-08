// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;

namespace osu.Framework.Tests.Layout.NonContainerTests
{
    [TestFixture]
    public class ValidationTest : LayoutTest
    {
        /// <summary>
        /// Tests that a box will never perform validations.
        /// </summary>
        [Test]
        public void TestBoxNeverValidates()
        {
            bool validated = false;
            var box = new TestBox { LayoutValidated = () => validated = true };

            Run(box, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsFalse(validated, "box should not have been validated");
                return true;
            });
        }
    }
}
