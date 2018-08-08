// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class ValidationTest : LayoutTest
    {
        /// <summary>
        /// Tests that a fixed-size container will never perform validations upon request.
        /// </summary>
        [Test]
        public void TestFixedSizeNeverValidates()
        {
            bool validated = false;
            var container = new TestContainer { LayoutValidated = () => validated = true };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsFalse(validated, "container should not be validated");
                return true;
            });
        }

        /// <summary>
        /// Tests that an auto-size container will perform validations upon request.
        /// </summary>
        [Test]
        [Ignore("Autosize validations aren't implemented yet.")]
        public void TestValidationValidatesAutoSize()
        {
            bool validated = false;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(validated, "container should be validated");
                return true;
            });
        }

        /// <summary>
        /// Tests that a previously-validated auto-size container will not re-validate.
        /// </summary>
        [Test]
        public void TestValidatedAutoSizeWillNotRevalidate()
        {
            bool validated = false;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                LayoutValidated = () => validated = true
            };

            Run(container, i =>
            {
                if (i < 2)
                {
                    validated = false;
                    return false;
                }

                Assert.IsFalse(validated, "container should not re-validate");
                return true;
            });

            container.ValidateSubTree();


        }

        /// <summary>
        /// Tests that the validation of an auto-size container validates its size dependencies.
        /// </summary>
        [Test]
        public void TestAutoSizeValidation()
        {
            var container = new TestContainer { AutoSizeAxes = Axes.Both };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "size dependencies should be valid");
                return true;
            });
        }

        /// <summary>
        /// Tests that the subtree validation of nested auto-size containers validates all size dependencies.
        /// </summary>
        [Test]
        public void TestNestedAutoSizeValidatesSizes()
        {
            TestContainer innerContainer;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = innerContainer = new TestContainer { AutoSizeAxes = Axes.Both }
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container size dependencies should validate");
                Assert.IsTrue(innerContainer.ChildrenSizeDependencies.IsValid, "inner container size dependencies should validate");
                return true;
            });
        }
    }
}
