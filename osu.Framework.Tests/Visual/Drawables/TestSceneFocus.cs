// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneFocus : ManualInputManagerTestScene
    {
        public FocusOverlay Overlay { get; private set; }
        public RequestingFocusBox RequestingFocus { get; private set; }

        public FocusBox FocusTopLeft { get; private set; }
        public FocusBox FocusBottomLeft { get; private set; }
        public FocusBox FocusBottomRight { get; private set; }

        public void SetUpScene()
        {
            Children = new Drawable[]
            {
                FocusTopLeft = new FocusBox
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                },
                RequestingFocus = new RequestingFocusBox
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
                FocusBottomLeft = new FocusBox
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                },
                FocusBottomRight = new FocusBox
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                },
                Overlay = new FocusOverlay
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            };
        }

        public class FocusOverlay : FocusedOverlayContainer
        {
            private readonly Box box;
            private readonly SpriteText stateText;

            public FocusOverlay()
            {
                RelativeSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray.Opacity(0.5f),
                    },
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Blue,
                    },
                    new SpriteText
                    {
                        Text = "FocusedOverlay",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    stateText = new SpriteText
                    {
                        Text = "FocusedOverlay",
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                    }
                };

                this.FadeTo(0.2f);
            }

            protected override void PopIn()
            {
                base.PopIn();
                stateText.Text = State.ToString();
            }

            protected override void PopOut()
            {
                base.PopOut();
                stateText.Text = State.ToString();
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnClick(ClickEvent e)
            {
                if (!box.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                {
                    Hide();
                    return true;
                }

                return base.OnClick(e);
            }

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                this.FadeTo(1);
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                this.FadeTo(0.2f);
            }
        }

        public class RequestingFocusBox : FocusBox
        {
            public override bool RequestsFocus => true;

            public RequestingFocusBox()
            {
                Box.Colour = Color4.Green;

                AddInternal(new SpriteText
                {
                    Text = "RequestsFocus",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });
            }
        }

        public class FocusBox : CompositeDrawable
        {
            protected Box Box;
            public int KeyDownCount, KeyUpCount, JoystickPressCount, JoystickReleaseCount;

            public FocusBox()
            {
                AddInternal(Box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.5f,
                    Colour = Color4.Red
                });

                RelativeSizeAxes = Axes.Both;
                Size = new Vector2(0.4f);
            }

            protected override bool OnClick(ClickEvent e) => true;

            public override bool AcceptsFocus => true;

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                Box.FadeTo(1);
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                Box.FadeTo(0.5f);
            }

            // only KeyDown is blocking
            protected override bool OnKeyDown(KeyDownEvent e)
            {
                ++KeyDownCount;
                return true;
            }

            protected override bool OnKeyUp(KeyUpEvent e)
            {
                ++KeyUpCount;
                return base.OnKeyUp(e);
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                ++JoystickPressCount;
                return base.OnJoystickPress(e);
            }

            protected override bool OnJoystickRelease(JoystickReleaseEvent e)
            {
                ++JoystickReleaseCount;
                return base.OnJoystickRelease(e);
            }
        }
    }
}
