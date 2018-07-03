// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System;
using NUnit.Framework;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class ValidationTest
    {
        /// <summary>
        /// Tests that a fixed-size container will never perform validations upon request.
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
        /// Tests that an auto-size container will perform validations upon request.
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

        /// <summary>
        /// Tests that the validation of an auto-size container validates its size dependencies.
        /// </summary>
        [Test]
        public void Test4()
        {
            var container = new LoadedContainer { AutoSizeAxes = Axes.Both };

            container.ValidateSubTree();

            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "size dependencies should be valid");
        }

        /// <summary>
        /// Tests that the subtree validation of nested auto-size containers validates all size dependencies.
        /// </summary>
        [Test]
        public void Test5()
        {
            LoadedContainer innerContainer;
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = innerContainer = new LoadedContainer { AutoSizeAxes = Axes.Both }
            };

            container.UpdateChildrenLife();
            container.ValidateSubTree();

            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container size dependencies should validate");
            Assert.IsTrue(innerContainer.ChildrenSizeDependencies.IsValid, "inner container size dependencies should validate");
        }

        private class LoadedContainer : Container
        {
            public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");
            public void InvalidateChildrenSizeDependencies() => this.Invalidate("childrenSizeDependencies");

            public Action LayoutValidated;

            protected override void ValidateLayout()
            {
                base.ValidateLayout();
                LayoutValidated?.Invoke();
            }

            public LoadedContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }

            public new void UpdateChildrenLife() => base.UpdateChildrenLife();
        }
    }
}
