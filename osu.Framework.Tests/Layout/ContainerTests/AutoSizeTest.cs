// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class AutoSizeTest : LayoutTest
    {
        /// <summary>
        /// Tests that an auto-sized container invalidates its size dependencies when a child is added.
        /// </summary>
        [Test]
        public void Test1()
        {
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = new Box()
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                container.Add(new Box());
                Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that an auto-sized container invalidates its size dependencies when a child is removed.
        /// </summary>
        [Test]
        public void Test2()
        {
            Box box;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = box = new Box()
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                container.Remove(box);
                Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that an auto-sized container invalidates its size dependencies when a child performs an <see cref="Invalidation.All"/> invalidation.
        /// </summary>
        [Test]
        public void Test3()
        {
            Box child;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = child = new Box()
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                child.Invalidate();
                Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that an auto-sized container invalidates its size dependencies when a child's property is changed and performs a
        /// <see cref="Invalidation.RequiredParentSizeToFit"/> invalidation.
        /// </summary>
        /// <param name="propertyName">The property or field that is to be changed.</param>
        /// <param name="newValue">The value which the target property should take on.</param>
        /// <param name="unobservedProperties">Any properties which should be set but should NOT be taken into account for the test.</param>
        [TestCaseSource(nameof(child_property_cases))]
        public void Test4(string propertyName, object newValue, params KeyValuePair<string, object>[] unobservedProperties)
        {
            Box child;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = child = new Box { Alpha = 0 }
            };

            if (unobservedProperties != null)
                foreach (var kvp in unobservedProperties)
                    child.Set(kvp.Key, kvp.Value);

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                child.Set(propertyName, newValue);
                Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that only the most-nested auto-size container invalidates when its child performs a<see cref="Invalidation.RequiredParentSizeToFit"/> invalidation.
        /// </summary>
        [Test]
        public void Test5()
        {
            Box child;
            TestContainer innerContainer;
            var outerContainer = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = innerContainer = new TestContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = child = new Box()
                }
            };

            Run(outerContainer, i =>
            {
                if (i == 0)
                    return false;

                child.Width = 10;
                Assert.IsTrue(outerContainer.ChildrenSizeDependencies.IsValid, "invalidation should not propagate to outer container");
                Assert.IsFalse(innerContainer.ChildrenSizeDependencies.IsValid, "inner container should have been invalidated");

                return true;
            });
        }

        /// <summary>
        /// Tests that autosize layout is validated when its <see cref="CompositeDrawable.Width"/> or <see cref="CompositeDrawable.Height"/> are referenced.
        /// </summary>
        [Test]
        public void Test6()
        {
            const float expected_size = 10;

            Box child;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = child = new Box()
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                child.Size = new Vector2(expected_size);
                Assert.AreEqual(new Vector2(10), container.Size);
                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should be validated");

                return true;
            });
        }

        /// <summary>
        /// Tests that nested autosize layout is validated when an autosizing parent's <see cref="CompositeDrawable.Width"/> or <see cref="CompositeDrawable.Height"/> are referenced.
        /// </summary>
        [Test]
        public void Test7()
        {
            const float expected_size = 10;

            Box child;
            TestContainer innerContainer;
            var container = new TestContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = innerContainer = new TestContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = child = new Box()
                }
            };

            Run(container, i =>
            {
                if (i == 0)
                    return false;

                child.Size = new Vector2(expected_size);
                Assert.AreEqual(new Vector2(10), innerContainer.Size);
                Assert.AreEqual(new Vector2(10), container.Size);
                Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should be validated");
                Assert.IsTrue(innerContainer.ChildrenSizeDependencies.IsValid, "inner container should be validated");

                return true;
            });
        }

        private static readonly object[] child_property_cases =
        {
            new object[] { nameof(Drawable.Origin), Anchor.Centre, null },
            new object[] { nameof(Drawable.Anchor), Anchor.Centre, null },
            new object[] { nameof(Drawable.Position), Vector2.One, null },
            new object[] { nameof(Drawable.X), 0.5f, null },
            new object[] { nameof(Drawable.Y), 0.5f, null },
            new object[] { nameof(Drawable.RelativeSizeAxes), Axes.Both, null },
            new object[] { nameof(Drawable.Size), new Vector2(10), null },
            new object[] { nameof(Drawable.Width), 0.5f, null },
            new object[] { nameof(Drawable.Height), 0.5f, null },
            new object[] { nameof(Drawable.FillMode), FillMode.Fit, null },
            new object[] { nameof(Drawable.Scale), new Vector2(2), null },
            new object[] { nameof(Drawable.Margin), new MarginPadding(2), null },
            new object[] { nameof(Drawable.Colour), (ColourInfo)Color4.Black, null },
            new object[] { nameof(Drawable.Rotation), 0.5f, null },
            new object[] { nameof(Drawable.Shear), Vector2.One, null },
        };
    }
}
