// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneJoystick : FrameworkTestScene
    {
        public TestSceneJoystick()
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

            for (int i = 0; i < JoystickState.MAX_AXES; i++)
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
                        Font = FrameworkFont.Condensed,
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

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
            {
                if (e.Button != button)
                {
                    base.OnJoystickRelease(e);
                    return;
                }

                background.FadeOut(100);
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

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
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
                    base.OnJoystickRelease(e);
            }
        }

        private class JoystickAxisButtonHandler : CompositeDrawable
        {
            private readonly JoystickAxisSource trackedAxis;
            private readonly Container background;

            private readonly JoystickButton positiveAxisButton;
            private readonly JoystickButton negativeAxisButton;
            private readonly SpriteText rawValue;

            private readonly Box fill;

            public JoystickAxisButtonHandler(int trackedAxis)
            {
                this.trackedAxis = (JoystickAxisSource)trackedAxis;
                positiveAxisButton = JoystickButton.FirstAxisPositive + trackedAxis;
                negativeAxisButton = JoystickButton.FirstAxisNegative + trackedAxis;

                Size = new Vector2(100, 50);

                InternalChildren = new Drawable[]
                {
                    fill = new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Both,
                        Width = 0,
                        Colour = Color4.SkyBlue,
                    },
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderThickness = 3,
                        Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Transparent },
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"Axis {trackedAxis + 1}"
                    },
                    rawValue = new SpriteText
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Text = "-"
                    }
                };
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                if (e.Button == positiveAxisButton)
                    background.BorderColour = Color4.DarkGreen;
                else if (e.Button == negativeAxisButton)
                    background.BorderColour = Color4.DarkRed;

                return base.OnJoystickPress(e);
            }

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
            {
                if (e.Button == positiveAxisButton || e.Button == negativeAxisButton)
                    background.BorderColour = Color4.Transparent;
                else
                    base.OnJoystickRelease(e);
            }

            protected override bool OnJoystickAxisMove(JoystickAxisMoveEvent e)
            {
                if (e.Axis.Source == trackedAxis)
                {
                    rawValue.Text = e.Axis.Value.ToString("0.0000000");
                    fill.Width = e.Axis.Value / 2;
                    fill.Alpha = Math.Abs(e.Axis.Value) == 1 ? 1 : 0.6f;
                }

                return false;
            }
        }
    }
}
