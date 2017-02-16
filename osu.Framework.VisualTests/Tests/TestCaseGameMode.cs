// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.GameModes;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseGameMode : TestCase
    {
        public override string Name => @"GameMode";

        public override string Description => @"Test stackable game modes";

        public override void Reset()
        {
            base.Reset();

            Add(new TestGameMode());
        }

        class TestGameMode : GameMode
        {
            public int Sequence;
            private Button popButton;

            const int transition_time = 500;

            protected override void OnEntering(GameMode last)
            {
                if (last != null)
                {
                    //only show the pop button if we are entered form another gamemode.
                    popButton.Alpha = 1;
                }

                Content.MoveTo(new Vector2(0, -DrawSize.Y));
                Content.MoveTo(Vector2.Zero, transition_time, EasingTypes.OutQuint);
            }

            protected override bool OnExiting(GameMode next)
            {
                Content.MoveTo(new Vector2(0, -DrawSize.Y), transition_time, EasingTypes.OutQuint);
                return base.OnExiting(next);
            }

            protected override void OnSuspending(GameMode next)
            {
                Content.MoveTo(new Vector2(0, DrawSize.Y), transition_time, EasingTypes.OutQuint);
            }

            protected override void OnResuming(GameMode last)
            {
                Content.MoveTo(Vector2.Zero, transition_time, EasingTypes.OutQuint);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = new Color4(
                            Math.Max(0.5f, RNG.NextSingle()),
                            Math.Max(0.5f, RNG.NextSingle()),
                            Math.Max(0.5f, RNG.NextSingle()),
                            1),
                    },
                    new SpriteText
                    {
                        Text = $@"Mode {Sequence}",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        TextSize = 50,
                    },
                    popButton = new Button
                    {
                        Text = @"Pop",
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.1f),
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        Colour = Color4.Red,
                        Alpha = 0,
                        Action = delegate {
                            Exit();
                        }
                    },
                    new Button
                    {
                        Text = @"Push",
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.1f),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Colour = Color4.YellowGreen,
                        Action = delegate {
                            Push(new TestGameMode
                            {
                                Sequence = Sequence + 1,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                            });
                        }
                    }
                };
            }
        }
    }
}
