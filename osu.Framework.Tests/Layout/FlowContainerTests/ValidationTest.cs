// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;

namespace osu.Framework.Tests.Layout.FlowContainerTests
{
    [TestFixture]
    public class ValidationTest : LayoutTest
    {
        /// <summary>
        /// Tests that a fixed-size flow container correctly validates positions.
        /// </summary>
        [Test]
        public void TestFixedSizeValidatesLayout()
        {
            var box1 = new Box { Size = new Vector2(50) };
            var box2 = new Box { Size = new Vector2(50) };
            var flow = new TestFlowContainer
            {
                Size = new Vector2(100),
                Children = new[] { box1, box2 }
            };

            Run(flow, i =>
            {
                if (i == 0)
                    return false;

                Assert.AreEqual(Vector2.Zero, box1.Position);
                Assert.AreEqual(new Vector2(50, 0), box2.Position);
                return true;
            });
        }

        /// <summary>
        /// Tests that an auto-size flow container correctly validates positions and sizes.
        /// </summary>
        [Test]
        public void TestAutoSizeValidatesLayoutAndSize()
        {
            var box1 = new Box { Size = new Vector2(50) };
            var box2 = new Box { Size = new Vector2(50) };
            var flow = new TestFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { box1, box2 }
            };

            Run(flow, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(flow.ChildrenSizeDependencies.IsValid);
                Assert.IsTrue(flow.Layout.IsValid);
                Assert.AreEqual(new Vector2(50, 100), flow.Size);
                return true;
            });
        }

        /// <summary>
        /// Tests that layout in a double flow container hierarchy is correctly validated.
        /// </summary>
        [Test]
        public void TestNestedFlowValidatesLayoutsAndSizes()
        {
            var innerBox1 = new Box { Size = new Vector2(50) };
            var innerBox2 = new Box { Size = new Vector2(50) };
            var outerBox1 = new Box { Size = new Vector2(50) };

            var innerFlow = new TestFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { innerBox1, innerBox2 }
            };

            var outerFlow = new TestFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[] { innerFlow, outerBox1 }
            };

            Run(outerFlow, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(outerFlow.ChildrenSizeDependencies.IsValid);
                Assert.IsTrue(outerFlow.Layout.IsValid);
                Assert.IsTrue(innerFlow.ChildrenSizeDependencies.IsValid);
                Assert.IsTrue(innerFlow.Layout.IsValid);
                Assert.AreEqual(new Vector2(100, 100), outerFlow.Size);
                Assert.AreEqual(new Vector2(50, 100), innerFlow.Size);
                return true;
            });
        }
    }
}
