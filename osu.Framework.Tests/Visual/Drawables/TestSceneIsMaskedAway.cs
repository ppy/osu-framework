// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    [HeadlessTest]
    public class TestSceneIsMaskedAway : FrameworkTestScene
    {
        /// <summary>
        /// Tests that a box which is within the bounds of a parent is never masked away, regardless of whether the parent is masking or not.
        /// </summary>
        /// <param name="masking">Whether the box's parent is masking.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void TestBoxInBounds(bool masking)
        {
            Box box = null;

            AddStep("init", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Masking = masking,
                    Child = box = new Box()
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box which is outside the bounds of a parent is never masked away if the parent is not masking.
        /// </summary>
        [Test]
        public void TestBoxOutOfBoundsNoMasking()
        {
            Box box = null;

            AddStep("init", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Child = box = new Box { Position = new Vector2(-1) }
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box which is slightly outside the bounds of a masking parent is never masked away, regardless of its anchor/origin.
        /// Ensures that all screen-space calculations are current by the time <see cref="Drawable.IsMaskedAway"/> is calculated.
        /// </summary>
        /// <param name="anchor">The box's anchor in the masking parent.</param>
        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.TopRight)]
        [TestCase(Anchor.BottomLeft)]
        [TestCase(Anchor.BottomRight)]
        public void TestBoxSlightlyOutOfBoundsMasking(Anchor anchor)
        {
            Box box = null;

            AddStep("init", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Masking = true,
                    Child = box = new Box
                    {
                        Anchor = anchor,
                        Origin = anchor,
                        Size = new Vector2(10),
                        Position = new Vector2(anchor.HasFlagFast(Anchor.x0) ? -5 : 5, anchor.HasFlagFast(Anchor.y0) ? -5 : 5),
                    }
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box which is fully outside the bounds of a masking parent is always masked away, regardless of its anchor/origin.
        /// Ensures that all screen-space calculations are current by the time <see cref="Drawable.IsMaskedAway"/> is calculated.
        /// </summary>
        /// <param name="anchor">The box's anchor in the masking parent.</param>
        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.TopRight)]
        [TestCase(Anchor.BottomLeft)]
        [TestCase(Anchor.BottomRight)]
        public void TestBoxFullyOutOfBoundsMasking(Anchor anchor)
        {
            Box box = null;

            AddStep("init", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Masking = true,
                    Child = box = new Box
                    {
                        Anchor = anchor,
                        Origin = anchor,
                        Size = new Vector2(10),
                        Position = new Vector2(anchor.HasFlagFast(Anchor.x0) ? -20 : 20, anchor.HasFlagFast(Anchor.y0) ? -20 : 20),
                    }
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => box.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box is never masked away when it and its proxy are within the bounds of their parents, regardless of whether their parents
        /// are masking or not.
        /// </summary>
        /// <param name="boxMasking">Whether the box's parent is masking.</param>
        /// <param name="proxyMasking">Whether the proxy's parent is masking.</param>
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void TestBoxInBoundsWithProxyInBounds(bool boxMasking, bool proxyMasking)
        {
            var box = new Box();
            Drawable proxy = null;

            AddStep("init", () =>
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = boxMasking,
                        Child = box
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = proxyMasking,
                        Child = proxy = box.CreateProxy()
                    },
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
            AddAssert("Check proxy IsMaskedAway", () => !proxy.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box is never masked away when its proxy is within the bounds of its parent, even if the box is outside the bounds of its parent.
        /// </summary>
        /// <param name="masking">Whether the box's parent is masking. This does not affect the proxy's parent.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void TestBoxOutOfBoundsWithProxyInBounds(bool masking)
        {
            var box = new Box { Position = new Vector2(-1) };
            Drawable proxy = null;

            AddStep("init", () =>
            {
                Children = new[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = masking,
                        Child = box
                    },
                    proxy = box.CreateProxy()
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
            AddAssert("Check proxy IsMaskedAway", () => !proxy.IsMaskedAway);
        }

        /// <summary>
        /// Tests that a box is only masked away when its proxy is masked away.
        /// </summary>
        /// <param name="boxMasking">Whether the box's parent is masking.</param>
        /// <param name="proxyMasking">Whether the proxy's parent is masking.</param>
        /// <param name="shouldBeMaskedAway">Whether the box should be masked away.</param>
        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void TestBoxInBoundsWithProxyOutOfBounds(bool boxMasking, bool proxyMasking, bool shouldBeMaskedAway)
        {
            var box = new Box();
            Drawable proxy = null;

            AddStep("init", () =>
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = boxMasking,
                        Child = box
                    },
                    new Container
                    {
                        Position = new Vector2(10),
                        Size = new Vector2(200),
                        Masking = proxyMasking,
                        Child = proxy = box.CreateProxy()
                    },
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => box.IsMaskedAway == shouldBeMaskedAway);
            AddAssert("Check proxy IsMaskedAway", () => !proxy.IsMaskedAway);
        }

        /// <summary>
        /// Tests that whether the box is out of bounds of its parent is not a consideration for masking, only whether its proxy is out of bounds of its parent.
        /// </summary>
        /// <param name="boxMasking">Whether the box's parent is masking.</param>
        /// <param name="proxyMasking">Whether the proxy's parent is masking.</param>
        /// <param name="shouldBeMaskedAway">Whether the box should be masked away</param>
        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void TestBoxOutOfBoundsWithProxyOutOfBounds(bool boxMasking, bool proxyMasking, bool shouldBeMaskedAway)
        {
            var box = new Box { Position = new Vector2(-1) };
            Drawable proxy = null;

            AddStep("init", () =>
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = boxMasking,
                        Child = box
                    },
                    new Container
                    {
                        Position = new Vector2(10),
                        Size = new Vector2(200),
                        Masking = proxyMasking,
                        Child = proxy = box.CreateProxy()
                    },
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => box.IsMaskedAway == shouldBeMaskedAway);
            AddAssert("Check proxy IsMaskedAway", () => !proxy.IsMaskedAway);
        }

        /// <summary>
        /// Tests that the box doesn't get masked away unless its most-proxied-proxy is masked away.
        /// In this case, the most-proxied-proxy is never going to be masked away, because it is within the bounds of its parent.
        /// </summary>
        /// <param name="boxMasking">Whether the box's parent is masking.</param>
        /// <param name="proxy1Masking">Whether the parent of box's proxy is masking.</param>
        /// <param name="proxy2Masking">Whether the parent of the proxy's proxy is masking.</param>
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, true, true)]
        [TestCase(false, true, true)]
        public void TestBoxInBoundsWithProxy1OutOfBoundsWithProxy2InBounds(bool boxMasking, bool proxy1Masking, bool proxy2Masking)
        {
            var box = new Box();
            var proxy1 = box.CreateProxy();
            var proxy2 = proxy1.CreateProxy();

            AddStep("init", () =>
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = boxMasking,
                        Child = box
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Position = new Vector2(10),
                        Masking = proxy1Masking,
                        Child = proxy1
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = proxy2Masking,
                        Child = proxy2
                    }
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => !box.IsMaskedAway);
            AddAssert("Check proxy1 IsMaskedAway", () => !proxy1.IsMaskedAway);
            AddAssert("Check proxy2 IsMaskedAway", () => !proxy2.IsMaskedAway);
        }

        /// <summary>
        /// Tests that whether the box is out of bounds of its parent is not a consideration for masking, only whether its most-proxied-proxy is out of bounds of its parent,
        /// and the most-proxied-proxy's parent is masking.
        /// </summary>
        /// <param name="boxMasking">Whether the box's parent is masking.</param>
        /// <param name="proxy1Masking">Whether the parent of box's proxy is masking.</param>
        /// <param name="proxy2Masking">Whether the parent of the proxy's proxy is masking.</param>
        /// <param name="shouldBeMaskedAway">Whether the box should be masked away.</param>
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        [TestCase(false, true, true, true)]
        [TestCase(false, false, true, true)]
        public void TestBoxInBoundsWithProxy1OutOfBoundsWithProxy2OutOfBounds(bool boxMasking, bool proxy1Masking, bool proxy2Masking, bool shouldBeMaskedAway)
        {
            var box = new Box();
            var proxy1 = box.CreateProxy();
            var proxy2 = proxy1.CreateProxy();

            AddStep("init", () =>
            {
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = boxMasking,
                        Child = box
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Masking = proxy1Masking,
                        Child = proxy1
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Position = new Vector2(10),
                        Masking = proxy2Masking,
                        Child = proxy2
                    }
                };
            });

            AddWaitStep("Wait for UpdateSubTree", 1);
            AddAssert("Check box IsMaskedAway", () => box.IsMaskedAway == shouldBeMaskedAway);
            AddAssert("Check proxy1 IsMaskedAway", () => !proxy1.IsMaskedAway);
            AddAssert("Check proxy2 IsMaskedAway", () => !proxy2.IsMaskedAway);
        }
    }
}
