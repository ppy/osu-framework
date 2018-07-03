// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class ValidationTest
    {
        /// <summary>
        /// Tests that a fixed-size container will never perform validations.
        /// </summary>
        [Test]
        public void Test1()
        {
            bool validated = false;
            var container = new LoadedContainer { LayoutValidated = () => validated = true };

            container.ValidateSubTree();
            Assert.IsFalse(validated, "container should not be validated");
        }

        /// <summary>
        /// Tests that an auto-size container will perform validations.
        /// </summary>
        [Test]
        public void Test2()
        {
            bool validated = false;
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            container.ValidateSubTree();
            Assert.IsTrue(validated, "container should be validated");
        }

        /// <summary>
        /// Tests that a previously-validated auto-size container will not re-validate.
        /// </summary>
        [Test]
        public void Test3()
        {
            bool validated = false;
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            container.ValidateSubTree();

            validated = false;
            container.ValidateSubTree();
            Assert.IsFalse(validated, "container should not re-validate");
        }

        private class LoadedContainer : Container
        {
            public Action LayoutValidated;

            protected override void UpdateLayout()
            {
                base.UpdateLayout();
                LayoutValidated?.Invoke();
            }

            public LoadedContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }
    }
}
