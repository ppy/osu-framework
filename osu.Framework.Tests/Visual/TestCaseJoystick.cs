// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseJoystick : TestCase
    {
        public TestCaseJoystick()
        {
            var buttonFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            var axisFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y
            };

            for (int i = 0; i < 20; i++)
                buttonFlow.Add(new JoystickButtonHandler(i));

            for (int i = 0; i < 6; i++)
                axisFlow.Add(new JoystickAxisHandler(i));

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { buttonFlow, axisFlow }
            };
        }

        private class JoystickButtonHandler : CompositeDrawable
        {
            private readonly Drawable background;

            private readonly int buttonIndex;

            public JoystickButtonHandler(int buttonIndex)
            {
                this.buttonIndex = buttonIndex;

                Size = new Vector2(100);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"Button {buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnJoystickPress(InputState state, JoystickPressEventArgs args)
            {
                if (args.Button != buttonIndex)
                    return base.OnJoystickPress(state, args);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override bool OnJoystickRelease(InputState state, JoystickReleaseEventArgs args)
            {
                if (args.Button != buttonIndex)
                    return base.OnJoystickRelease(state, args);

                background.FadeOut(100);
                return true;
            }
        }

        private class JoystickAxisHandler : CompositeDrawable
        {
            private readonly Drawable background;
            private readonly SpriteText value;

            private readonly int axisIndex;

            public JoystickAxisHandler(int axisIndex)
            {
                this.axisIndex = axisIndex;

                Size = new Vector2(200);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Transparent,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new[]
                        {
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = $"Axis {axisIndex + 1}"
                            },
                            value = new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                            }
                        }
                    }
                };
            }

            protected override bool OnJoystickAxisIncrease(InputState state, JoystickAxisEventArgs args)
            {
                if (args.Axis != axisIndex)
                    return base.OnJoystickAxisIncrease(state, args);

                updateColour(state);

                value.Text = $"{state.Joystick.Axes[axisIndex]} (+{state.Joystick.AxisDelta(axisIndex)})";
                return true;
            }

            protected override bool OnJoystickAxisDecrease(InputState state, JoystickAxisEventArgs args)
            {
                if (args.Axis != axisIndex)
                    return base.OnJoystickAxisDecrease(state, args);

                updateColour(state);

                value.Text = $"{state.Joystick.Axes[axisIndex]} ({state.Joystick.AxisDelta(axisIndex)})";
                return true;
            }

            private void updateColour(InputState state)
            {
                Color4 targetColour;

                if (Precision.DefinitelyBigger(state.Joystick.Axes[axisIndex], 0))
                    targetColour = state.Joystick.AxisDelta(axisIndex) > 0 ? Color4.DarkGreen : Color4.DarkRed;
                else
                    targetColour = new Color4(0, 0, 0, 0);

                background.FadeColour(targetColour, 100, Easing.OutQuint);
            }
        }
    }
}
