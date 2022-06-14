// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneFrontToBackBox : FrameworkTestScene
    {
        [Resolved]
        private FrameworkDebugConfigManager debugConfig { get; set; }

        private TestBox blendedBox;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Clear();
        });

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddToggleStep("disable front to back", val => debugConfig.SetValue(DebugSetting.BypassFrontToBackPass, val));
        }

        [Test]
        public void TestOpaqueBoxWithMixedBlending()
        {
            createBlendedBox();
            AddAssert("renders interior", () => blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestTransparentBoxWithMixedBlending()
        {
            createBlendedBox(b => b.Alpha = 0.5f);
            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestOpaqueBoxWithAdditiveBlending()
        {
            createBlendedBox(b => b.Blending = BlendingParameters.Additive);
            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestTransparentBoxWithAdditiveBlending()
        {
            createBlendedBox(b =>
            {
                b.Blending = BlendingParameters.Additive;
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
            createBlendedBox(b =>
            {
                var blending = BlendingParameters.Inherit;
                blending.RGBEquation = equationMode;
                b.Blending = blending;
            });

            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [TestCase(BlendingEquation.Max)]
        [TestCase(BlendingEquation.Min)]
        [TestCase(BlendingEquation.Subtract)]
        [TestCase(BlendingEquation.ReverseSubtract)]
        public void TestOpaqueBoxWithNonAddAlphaEquation(BlendingEquation equationMode)
        {
            createBlendedBox(b =>
            {
                var blending = BlendingParameters.Inherit;
                blending.AlphaEquation = equationMode;
                b.Blending = blending;
            });

            AddAssert("doesn't render interior", () => !blendedBox.CanDrawOpaqueInterior);
        }

        [Test]
        public void TestSmallSizeMasking()
        {
            AddStep("create test", () =>
            {
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -120,
                        Text = "No rounded corners should be visible below",
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(20),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Green
                            },
                            new Container
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 40,
                                Child = new Box
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Green
                                }
                            }
                        }
                    }
                };
            });
        }

        [Test]
        public void TestNegativeSizeMasking()
        {
            AddStep("create test", () =>
            {
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -120,
                        Text = "No rounded corners should be visible below",
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(200),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Green
                            },
                            new Container
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(-1f),
                                Masking = true,
                                CornerRadius = 20,
                                Child = new Box
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Green
                                }
                            }
                        }
                    }
                };
            });
        }

        private void createBlendedBox(Action<Box> setupAction = null) => AddStep("create box", () =>
        {
            Child = new Container
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
            };

            setupAction?.Invoke(blendedBox);
        });

        private class TestBox : Box
        {
            public bool CanDrawOpaqueInterior => currentDrawNode.CanDrawOpaqueInterior;

            private DrawNode currentDrawNode;

            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode(this);

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
                => currentDrawNode = base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

            private class TestBoxDrawNode : SpriteDrawNode
            {
                public TestBoxDrawNode(Box source)
                    : base(source)
                {
                }
            }
        }
    }
}
