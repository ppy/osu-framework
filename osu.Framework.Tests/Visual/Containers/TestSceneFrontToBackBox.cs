// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneFrontToBackBox : FrameworkTestScene
    {
        private TestBox blendedBox;

        [BackgroundDependencyLoader]
        private void load(FrameworkDebugConfigManager debugConfig)
        {
            AddToggleStep("disable front to back", val => debugConfig.Set(DebugSetting.BypassFrontToBackPass, val));
        }

        [Test]
        public void TestOpaqueBoxWithMixedBlending()
        {
            createBox();
            AddAssert("renders interior", () => blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestTransparentBoxWithMixedBlending()
        {
            createBox(b => b.Alpha = 0.5f);
            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestOpaqueBoxWithAdditiveBlending()
        {
            createBox(b => b.Blending = BlendingMode.Additive);
            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestTransparentBoxWithAdditiveBlending()
        {
            createBox(b =>
            {
                b.Blending = BlendingMode.Additive;
                b.Alpha = 0.5f;
            });

            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [TestCase(BlendingEquation.Max)]
        [TestCase(BlendingEquation.Min)]
        [TestCase(BlendingEquation.Subtract)]
        [TestCase(BlendingEquation.ReverseSubtract)]
        public void TestOpaqueBoxWithNonAddRGBEquation(BlendingEquation equationMode)
        {
            createBox(b =>
            {
                b.Blending = new BlendingParameters
                {
                    Mode = BlendingMode.Inherit,
                    AlphaEquation = BlendingEquation.Inherit,
                    RGBEquation = equationMode
                };
            });

            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [TestCase(BlendingEquation.Max)]
        [TestCase(BlendingEquation.Min)]
        [TestCase(BlendingEquation.Subtract)]
        [TestCase(BlendingEquation.ReverseSubtract)]
        public void TestOpaqueBoxWithNonAddAlphaEquation(BlendingEquation equationMode)
        {
            createBox(b =>
            {
                b.Blending = new BlendingParameters
                {
                    Mode = BlendingMode.Inherit,
                    AlphaEquation = equationMode,
                    RGBEquation = BlendingEquation.Inherit
                };
            });

            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        private void createBox(Action<Box> setupAction = null) => AddStep("create box", () =>
        {
            Clear();

            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(50, 50, 50, 255)
                    },
                    blendedBox = new TestBox
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(100, 100, 100, 255),
                        Size = new Vector2(0.5f),
                    }
                }
            });

            setupAction?.Invoke(blendedBox);
        });

        private class TestBox : Box
        {
            public bool CanDrawOpaqueInterior => currentDrawNode.CanDrawOpaqueInterior;

            private DrawNode currentDrawNode;

            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode(this);

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
                => currentDrawNode = base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

            private class TestBoxDrawNode : BoxDrawNode
            {
                public TestBoxDrawNode(Box source)
                    : base(source)
                {
                }
            }
        }
    }
}
