// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneEasingCurves : TestScene
    {
        private const float default_size = 200;

        private FillFlowContainer easingsContainer;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            var easingTypes = Enum.GetValues(typeof(Easing))
                                  .OfType<Easing>()
                                  .ToList();

            Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = easingsContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = easingTypes.Select(type => new Visualiser(type)).ToArray()
                }
            };
        });

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddSliderStep("resize easings", default_size / 2, default_size * 2, default_size, size =>
            {
                easingsContainer?.Children?.OfType<Visualiser>().ForEach(easing => easing.ResizeTo(new Vector2(size)));
            });
        }

        private class Visualiser : Container
        {
            private const float movement_duration = 1000f;
            private const float pause_duration = 200f;

            public readonly Easing Easing;

            private readonly CircularContainer dot;

            public Visualiser(Easing easing)
            {
                Easing = easing;

                Size = new Vector2(default_size);
                Padding = new MarginPadding(5);

                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = Color4.DimGray,
                                RelativeSizeAxes = Axes.Both
                            },
                            dot = new CircularContainer
                            {
                                Origin = Anchor.Centre,
                                RelativePositionAxes = Axes.Both,
                                Size = new Vector2(10),
                                Masking = true,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.White
                                }
                            }
                        }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 10,
                        Text = easing.ToString()
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                dot.MoveToX(1.0f, movement_duration, Easing)
                   .Then(pause_duration)
                   .MoveToX(0.0f, movement_duration, Easing)
                   .Loop(pause_duration);
                dot.MoveToY(1.0f, movement_duration)
                   .Then(pause_duration)
                   .MoveToY(0.0f, movement_duration)
                   .Loop(pause_duration);
            }
        }
    }
}
