// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.GameModes;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using OpenTK;
using OpenTK.Graphics;

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

            protected override double OnEntering(GameMode last)
            {
                if (last != null)
                {
                    //only show the pop button if we are entered form another gamemode.
                    popButton.Alpha = 1;
                }

                Content.MoveTo(new Vector2(0, -Size.Y));
                Content.MoveTo(Vector2.Zero, transition_time, EasingTypes.OutQuint);
                return transition_time;
            }

            protected override double OnExiting(GameMode next)
            {
                Content.MoveTo(new Vector2(0, -Size.Y), transition_time, EasingTypes.OutQuint);
                return transition_time;
            }

            protected override double OnSuspending(GameMode next)
            {
                Content.MoveTo(new Vector2(0, Size.Y), transition_time, EasingTypes.OutQuint);
                return transition_time;
            }

            protected override double OnResuming(GameMode last)
            {
                Content.MoveTo(Vector2.Zero, transition_time, EasingTypes.OutQuint);
                return transition_time;
            }

            public override void Load()
            {
                base.Load();

                Children = new Drawable[]
                {
                    new Box
                    {
                        SizeMode = InheritMode.XY,
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
                        SizeMode = InheritMode.XY,
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
                        SizeMode = InheritMode.XY,
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
