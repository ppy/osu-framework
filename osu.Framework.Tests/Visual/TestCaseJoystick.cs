// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

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

            var hatFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y
            };

            var axisFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y
            };

            for (int i = 0; i < 64; i++)
                buttonFlow.Add(new JoystickButtonHandler(i));

            for (int i = 0; i < 4; i++)
                hatFlow.Add(new JoystickHatHandler(i));

            for (int i = 0; i < 64; i++)
                axisFlow.Add(new JoystickAxisButtonHandler(i));

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { buttonFlow, hatFlow, axisFlow }
            };
        }

        private class JoystickButtonHandler : CompositeDrawable
        {
            private readonly Drawable background;

            private readonly JoystickButton button;

            public JoystickButtonHandler(int buttonIndex)
            {
                button = JoystickButton.FirstButton + buttonIndex;

                Size = new Vector2(50);

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
                        Text = $"B{buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                if (e.Button != button)
                    return base.OnJoystickPress(e);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override bool OnJoystickRelease(JoystickReleaseEvent e)
            {
                if (e.Button != button)
                    return base.OnJoystickRelease(e);

                background.FadeOut(100);
                return true;
            }
        }

        private class JoystickHatHandler : CompositeDrawable
        {
            private readonly Drawable upBox;
            private readonly Drawable downBox;
            private readonly Drawable leftBox;
            private readonly Drawable rightBox;

            private readonly int hatIndex;

            public JoystickHatHandler(int hatIndex)
            {
                this.hatIndex = hatIndex;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    upBox = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Colour = Color4.DarkGreen,
                        Height = 10,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    downBox = new Container
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.X,
                        Colour = Color4.DarkGreen,
                        Height = 10,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    leftBox = new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Colour = Color4.DarkGreen,
                        Width = 10,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    rightBox = new Container
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Y,
                        Colour = Color4.DarkGreen,
                        Width = 10,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"H{hatIndex + 1}"
                    }
                };
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                if (e.Button == JoystickButton.FirstHatUp + hatIndex)
                    upBox.FadeIn(100, Easing.OutQuint);
                else if (e.Button == JoystickButton.FirstHatDown + hatIndex)
                    downBox.FadeIn(100, Easing.OutQuint);
                else if (e.Button == JoystickButton.FirstHatLeft + hatIndex)
                    leftBox.FadeIn(100, Easing.OutQuint);
                else if (e.Button == JoystickButton.FirstHatRight + hatIndex)
                    rightBox.FadeIn(100, Easing.OutQuint);
                else
                    return base.OnJoystickPress(e);

                return true;
            }

            protected override bool OnJoystickRelease(JoystickReleaseEvent e)
            {
                if (e.Button == JoystickButton.FirstHatUp + hatIndex)
                    upBox.FadeOut(100);
                else if (e.Button == JoystickButton.FirstHatDown + hatIndex)
                    downBox.FadeOut(100);
                else if (e.Button == JoystickButton.FirstHatLeft + hatIndex)
                    leftBox.FadeOut(100);
                else if (e.Button == JoystickButton.FirstHatRight + hatIndex)
                    rightBox.FadeOut(100);
                else
                    return base.OnJoystickRelease(e);

                return true;
            }
        }

        private class JoystickAxisButtonHandler : CompositeDrawable
        {
            private readonly int axisIndex;
            private readonly Drawable background;

            private readonly JoystickButton positiveAxisButton;
            private readonly JoystickButton negativeAxisButton;
            private readonly SpriteText rawValue;

            public JoystickAxisButtonHandler(int axisIndex)
            {
                this.axisIndex = axisIndex;
                positiveAxisButton = JoystickButton.FirstAxisPositive + axisIndex;
                negativeAxisButton = JoystickButton.FirstAxisNegative + axisIndex;

                Size = new Vector2(50);

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
                        Text = $"AX{axisIndex + 1}"
                    },
                    rawValue = new SpriteText
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Text = "-"
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                var joy = GetContainingInputManager().CurrentState.Joystick;
                rawValue.Text = joy.Axes.Find(a => a.Axis == axisIndex).Value.ToString("0.00");
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                if (e.Button == positiveAxisButton)
                    background.FadeColour(Color4.DarkGreen, 100, Easing.OutQuint);
                else if (e.Button == negativeAxisButton)
                    background.FadeColour(Color4.DarkRed, 100, Easing.OutQuint);
                else
                    return base.OnJoystickPress(e);
                return true;
            }

            protected override bool OnJoystickRelease(JoystickReleaseEvent e)
            {
                if (e.Button == positiveAxisButton || e.Button == negativeAxisButton)
                    background.FadeColour(new Color4(0, 0, 0, 0), 100, Easing.OutQuint);
                else
                    return base.OnJoystickRelease(e);
                return true;
            }
        }
    }
}
