// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.NonVisual
{
    public class LayoutValidationTest
    {


        [Test]
        public void TestNonAutoSizingContainerDoesNotValidateLayout()
        {
            bool validated = false;
            var container = new Container1 { LayoutValidated = () => validated = true };

            container.ValidateSubTree();
            Assert.IsFalse(validated, "container should not be validated");
        }

        private class Container1 : Container
        {
            public Action LayoutValidated;

            protected override void UpdateLayout()
            {
                base.UpdateLayout();
                LayoutValidated?.Invoke();
            }
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
