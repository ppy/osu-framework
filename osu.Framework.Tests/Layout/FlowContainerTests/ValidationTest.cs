// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;

namespace osu.Framework.Tests.Layout.FlowContainerTests
{
    [TestFixture]
    public class ValidationTest
    {
        /// <summary>
        /// Tests that a fixed-size flow container correctly validates positions.
        /// </summary>
        [Test]
        public void Test1()
        {
            var box1 = new LoadedBox { Size = new Vector2(50) };
            var box2 = new LoadedBox { Size = new Vector2(50) };
            var flow = new LoadedFlowContainer
            {
                Size = new Vector2(100),
                Children = new[] { box1, box2 }
            };

            flow.UpdateChildrenLife();
            flow.ValidateSubTree();

            Assert.AreEqual(Vector2.Zero, box1.Position);
            Assert.AreEqual(new Vector2(50, 0), box2.Position);
        }

        /// <summary>
        /// Tests that an auto-size flow container correctly validates positions and sizes.
        /// </summary>
        [Test]
        [Ignore("Autosize validations aren't implemented yet.")]
        public void Test2()
        {
            var box1 = new LoadedBox { Size = new Vector2(50) };
            var box2 = new LoadedBox { Size = new Vector2(50) };
            var flow = new LoadedFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { box1, box2 }
            };

            flow.UpdateChildrenLife();
            flow.ValidateSubTree();

            Assert.IsTrue(flow.ChildrenSizeDependencies.IsValid);
            Assert.IsTrue(flow.Layout.IsValid);
            Assert.AreEqual(new Vector2(50, 100), flow.Size);
        }

        /// <summary>
        /// Tests that layout in a double flow container hierarchy is correctly validated.
        /// </summary>
        [Test]
        [Ignore("Autosize validations aren't implemented yet.")]
        public void Test3()
        {
            var innerBox1 = new LoadedBox { Size = new Vector2(50) };
            var innerBox2 = new LoadedBox { Size = new Vector2(50) };
            var outerBox1 = new LoadedBox { Size = new Vector2(50) };

            var innerFlow = new LoadedFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { innerBox1, innerBox2 }
            };

            var outerFlow = new LoadedFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[] { innerFlow, outerBox1 }
            };

            outerFlow.UpdateChildrenLife();
            innerFlow.UpdateChildrenLife();

            outerFlow.ValidateSubTree();

            Assert.IsTrue(outerFlow.ChildrenSizeDependencies.IsValid);
            Assert.IsTrue(outerFlow.Layout.IsValid);
            Assert.IsTrue(innerFlow.ChildrenSizeDependencies.IsValid);
            Assert.IsTrue(innerFlow.Layout.IsValid);
            Assert.AreEqual(new Vector2(100, 100), outerFlow.Size);
            Assert.AreEqual(new Vector2(50, 100), innerFlow.Size);
        }

        private class LoadedContainer : Container
        {
            public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");

            public LoadedContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }

            public new void UpdateChildrenLife() => base.UpdateChildrenLife();
        }

        private class LoadedFlowContainer : FillFlowContainer
        {
            public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");
            public Cached Layout => this.Get<Cached>("layout");

            public LoadedFlowContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }

            public new void UpdateChildrenLife() => base.UpdateChildrenLife();
        }

        private class LoadedBox : Box
        {
            public LoadedBox()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }
    }
}
