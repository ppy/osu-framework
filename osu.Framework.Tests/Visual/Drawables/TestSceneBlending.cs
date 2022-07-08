// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneBlending : FrameworkTestScene
    {
        [Test]
        public void TestBlendSelf()
        {
            Drawable blended = null;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Children = new[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Orange
                        },
                        blended = new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(50),
                            Alpha = 0.5f,
                            Blending = BlendingParameters.Additive
                        }
                    }
                };
            });

            AddAssert("blended additively", () => blended.DrawColourInfo.Blending == BlendingParameters.Additive);
        }

        [Test]
        public void TestBlendThroughMultipleParents()
        {
            Drawable blended = null;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    Size = new Vector2(200),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Orange
                        },
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(50),
                            Blending = BlendingParameters.Additive,
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = blended = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0.5f,
                                }
                            }
                        }
                    }
                };
            });

            AddAssert("blended additively", () => blended.DrawColourInfo.Blending == BlendingParameters.Additive);
        }
    }
}
