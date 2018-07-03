// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Layout.NonContainerTests
{
    [TestFixture]
    public class ValidationTest
    {
        /// <summary>
        /// Tests that a box will never perform validations.
        /// </summary>
        [Test]
        public void Test1()
        {
            bool validated = false;
            var box = new Box1 { LayoutValidated = () => validated = true };

            box.ValidateSubTree();
            Assert.IsFalse(validated, "box should not have been validated");
        }

        private class Box1 : Box
        {
            public Action LayoutValidated;

            protected override void UpdateLayout()
            {
                base.UpdateLayout();
                LayoutValidated?.Invoke();
            }
        }
    }
}
