// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneBackdropBlur : TestSceneMasking
    {
        public TestSceneBackdropBlur()
        {
            Remove(TestContainer, false);

            BackdropBlurContainer buffer;
            Path path;

            Add(new BufferedContainer
            {
                BackgroundColour = FrameworkColour.YellowGreenDark,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    TestContainer,
                    buffer = new BackdropBlurContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        Padding = new MarginPadding(100),
                        Children = new[]
                        {
                            path = new GradientPath
                            {
                                PathRadius = 50,
                                Vertices = new[]
                                {
                                    new Vector2(0, 0),
                                    new Vector2(150, 50),
                                    new Vector2(250, -25),
                                    new Vector2(400, 25)
                                }
                            }
                        }
                    }
                }
            });

            AddSliderStep("blur", 0f, 20f, 5f, blur =>
            {
                buffer.BlurTo(new Vector2(blur));
            });

            AddSliderStep("container alpha", 0f, 1f, 1f, alpha =>
            {
                buffer.Alpha = alpha;
            });

            AddSliderStep("child alpha", 0f, 1f, 0.5f, alpha =>
            {
                path.Alpha = alpha;
            });

            AddSliderStep("mask cutoff", 0f, 1f, 0.0f, cutoff =>
            {
                buffer.MaskCutoff = cutoff;
            });

            AddSliderStep("fbo scale (x)", 0.01f, 4f, 1f, scale =>
            {
                buffer.EffectBufferScale = buffer.FrameBufferScale with { X = scale };
            });

            AddSliderStep("fbo scale (y)", 0.01f, 4f, 1f, scale =>
            {
                buffer.EffectBufferScale = buffer.FrameBufferScale with { Y = scale };
            });
        }

        private partial class GradientPath : SmoothPath
        {
            protected override Color4 ColourAt(float position)
            {
                return base.ColourAt(position) with { A = 0.5f + position * 0.5f };
            }
        }
    }
}
