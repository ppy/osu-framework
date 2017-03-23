// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens.Testing;
using osu.Framework.Input;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseCircularContainer : TestCase
    {
        public override string Description => "Non-masking (input only) + masking circular containers";

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = $"None of the folowing {nameof(CircularContainer)}s should trigger until the white part is hovered"
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Vertical,
                            AutoSizeAxes = Axes.Both,
                            Spacing = new Vector2(0, 2),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = "No masking"
                                },
                                new CircularContainerWithInput
                                {
                                    Size = new Vector2(200),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Red
                                        },
                                        new CircularContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.White,
                                            Masking = true,
                                            Children = new[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Vertical,
                            AutoSizeAxes = Axes.Both,
                            Spacing = new Vector2(0, 2),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = "With masking"
                                },
                                new CircularContainerWithInput
                                {
                                    Size = new Vector2(200),
                                    Masking = true,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Red
                                        },
                                        new CircularContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.White,
                                            Masking = true,
                                            Children = new[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private class CircularContainerWithInput : CircularContainer
        {
            protected override bool OnHover(InputState state)
            {
                ScaleTo(1.2f, 100);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                ScaleTo(1f, 100);
            }
        }
    }
}
