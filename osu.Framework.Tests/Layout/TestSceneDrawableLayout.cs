// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Layout
{
    [HeadlessTest]
    public class TestSceneDrawableLayout : FrameworkTestScene
    {
        /// <summary>
        /// Tests that multiple invalidations trigger for properties that don't overlap in their invalidation types (size + scale).
        /// </summary>
        [Test]
        public void TestChangeNonOverlappingProperties()
        {
            Box[] boxes = new Box[4];
            TestContainer1 testContainer = null;

            AddStep("create test", () =>
            {
                Child = testContainer = new TestContainer1
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.25f,
                        Children = new[]
                        {
                            boxes[0] = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Beige,
                                Width = 0.2f,
                            },
                            boxes[1] = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Bisque,
                                Width = 0.2f,
                            },
                            boxes[2] = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Aquamarine,
                                Width = 0.2f,
                            },
                            boxes[3] = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Cornsilk,
                                Width = 0.2f,
                            },
                        }
                    }
                };
            });

            AddWaitStep("wait for flow", 2);
            AddStep("change scale", () => testContainer.AdjustScale(0.5f));

            AddAssert("boxes flowed correctly", () =>
            {
                float expectedX = 0;

                foreach (var child in boxes)
                {
                    if (!Precision.AlmostEquals(expectedX, child.DrawPosition.X))
                        return false;

                    expectedX += child.DrawWidth;
                }

                return true;
            });
        }

        [Test]
        public void TestChangePositionInvalidatesMiscGeometryOnSelf()
        {
            TestBox1 box = null;

            AddStep("create test", () =>
            {
                Child = box = new TestBox1
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };
            });

            AddUntilStep("wait for validation", () => box.MiscGeometryLayoutValue.IsValid);

            AddAssert("change position and ensure MiscGeometry invalidated on self", () =>
            {
                box.Position = new Vector2(50);
                return !box.MiscGeometryLayoutValue.IsValid;
            });
        }

        [Test]
        public void TestChangeSizeInvalidatesDrawSizeOnSelf()
        {
            TestBox1 box = null;

            AddStep("create test", () =>
            {
                Child = box = new TestBox1
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(50)
                };
            });

            AddUntilStep("wait for validation", () => box.DrawSizeLayoutValue.IsValid);

            AddAssert("change size and ensure DrawSize invalidated on self", () =>
            {
                box.Size = new Vector2(100);
                return !box.DrawSizeLayoutValue.IsValid;
            });
        }

        private class TestContainer1 : Container<Drawable>
        {
            public void AdjustScale(float scale = 1.0f)
            {
                this.ScaleTo(new Vector2(scale));
                this.ResizeTo(new Vector2(1 / scale));
            }
        }

        private class TestBox1 : Box
        {
            public readonly LayoutValue MiscGeometryLayoutValue = new LayoutValue(Invalidation.MiscGeometry, InvalidationSource.Self);
            public readonly LayoutValue DrawSizeLayoutValue = new LayoutValue(Invalidation.DrawSize, InvalidationSource.Self);

            public TestBox1()
            {
                AddLayout(MiscGeometryLayoutValue);
                AddLayout(DrawSizeLayoutValue);
            }

            protected override void Update()
            {
                base.Update();

                MiscGeometryLayoutValue.Validate();
                DrawSizeLayoutValue.Validate();
            }
        }
    }
}
