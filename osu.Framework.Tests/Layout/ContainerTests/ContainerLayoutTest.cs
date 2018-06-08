// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class ContainerLayoutTest
    {
        [Test]
        public void TestNonAutoSizingContainerDoesNotValidateLayout()
        {
            bool validated = false;
            var container = new Container1 { LayoutValidated = () => validated = true };

            container.ValidateSubTree();
            Assert.IsFalse(validated, "container should not be validated");
        }

        [Test]
        public void TestAutoSizingContainerValidatesLayout()
        {
            bool validated = false;
            var container = new Container1
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            container.ValidateSubTree();
            Assert.IsTrue(validated, "container should be validated");
        }

        [Test]
        public void TestAutoSizingContainerCachesLayoutValidation()
        {
            bool validated = false;
            var container = new Container1
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            container.ValidateSubTree();
            validated = false;
            container.ValidateSubTree();
            Assert.IsFalse(validated, "container should be validated");
        }

        private class Container1 : Container
        {
            public Action LayoutValidated;
            public Action LayoutInvalidated;

            protected override void UpdateLayout()
            {
                base.UpdateLayout();
                LayoutValidated?.Invoke();
            }

            public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
            {
                base.InvalidateFromChild(invalidation, source);
                LayoutInvalidated?.Invoke();
            }
        }
    }
}
