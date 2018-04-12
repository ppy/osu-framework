// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
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
                axisFlow.Add(new JoystickAxisButtonHandler(i));

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

            private readonly JoystickButton button;

            public JoystickButtonHandler(int buttonIndex)
            {
                button = (JoystickButton)buttonIndex;

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

            protected override bool OnJoystickPress(InputState state, JoystickEventArgs args)
            {
                if (args.Button != button)
                    return base.OnJoystickPress(state, args);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args)
            {
                if (args.Button != button)
                    return base.OnJoystickRelease(state, args);

                background.FadeOut(100);
                return true;
            }
        }

        private class JoystickAxisButtonHandler : CompositeDrawable
        {
            private readonly Drawable background;

            private readonly JoystickButton positiveAxisButton;
            private readonly JoystickButton negativeAxisButton;

            public JoystickAxisButtonHandler(int axisIndex)
            {
                positiveAxisButton = JoystickButton.AxisPositive1 + axisIndex;
                negativeAxisButton = JoystickButton.AxisNegative1 + axisIndex;

                Size = new Vector2(200);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Transparent,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"Axis {axisIndex + 1}"
                    }
                };
            }

            protected override bool OnJoystickPress(InputState state, JoystickEventArgs args)
            {
                if (args.Button == positiveAxisButton)
                    background.FadeColour(Color4.DarkGreen, 100, Easing.OutQuint);
                else if (args.Button == negativeAxisButton)
                    background.FadeColour(Color4.DarkRed, 100, Easing.OutQuint);
                else
                    return base.OnJoystickPress(state, args);
                return true;
            }

            protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args)
            {
                if (args.Button == positiveAxisButton || args.Button == negativeAxisButton)
                    background.FadeColour(new Color4(0, 0, 0, 0), 100, Easing.OutQuint);
                else
                    return base.OnJoystickRelease(state, args);
                return true;
            }
        }
    }
}
