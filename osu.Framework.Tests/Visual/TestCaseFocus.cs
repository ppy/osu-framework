// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using JoystickEventArgs = osu.Framework.Input.EventArgs.JoystickEventArgs;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFocus : ManualInputManagerTestCase
    {
        private FocusOverlay overlay;
        private RequestingFocusBox requestingFocus;

        private FocusBox focusTopLeft;
        private FocusBox focusBottomLeft;
        private FocusBox focusBottomRight;

        public TestCaseFocus()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            Children = new Drawable[]
            {
                focusTopLeft = new FocusBox
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                },
                requestingFocus = new RequestingFocusBox
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
                focusBottomLeft = new FocusBox
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                },
                focusBottomRight = new FocusBox
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                },
                overlay = new FocusOverlay
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            };
        }

        [Test]
        public void FocusedOverlayTakesFocusOnShow()
        {
            AddAssert("overlay not visible", () => overlay.State == Visibility.Hidden);
            checkNotFocused(() => overlay);

            AddStep("show overlay", () => overlay.Show());
            checkFocused(() => overlay);

            AddStep("hide overlay", () => overlay.Hide());
            checkNotFocused(() => overlay);
        }

        [Test]
        public void FocusedOverlayLosesFocusOnClickAway()
        {
            AddAssert("overlay not visible", () => overlay.State == Visibility.Hidden);
            checkNotFocused(() => overlay);

            AddStep("show overlay", () => overlay.Show());
            checkFocused(() => overlay);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkNotFocused(() => overlay);
            checkFocused(() => requestingFocus);
        }

        [Test]
        public void RequestsFocusKeepsFocusOnClickAway()
        {
            checkFocused(() => requestingFocus);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => requestingFocus);
        }

        [Test]
        public void RequestsFocusLosesFocusOnClickingFocused()
        {
            checkFocused(() => requestingFocus);

            AddStep("click top left", () =>
            {
                InputManager.MoveMouseTo(focusTopLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusTopLeft);

            AddStep("click bottom right", () =>
            {
                InputManager.MoveMouseTo(focusBottomRight);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusBottomRight);
        }

        [Test]
        public void ShowOverlayInteractions()
        {
            AddStep("click bottom left", () =>
            {
                InputManager.MoveMouseTo(focusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => focusBottomLeft);

            AddStep("show overlay", () => overlay.Show());

            checkFocused(() => overlay);
            checkNotFocused(() => focusBottomLeft);

            // click is blocked by overlay so doesn't select bottom left first click
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => requestingFocus);

            // second click selects bottom left
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => focusBottomLeft);

            // further click has no effect
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => focusBottomLeft);
        }

        [Test]
        public void InputPropagation()
        {
            AddStep("Focus bottom left", () =>
            {
                InputManager.MoveMouseTo(focusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("Press a key (blocking)", () =>
            {
                InputManager.PressKey(Key.A);
                InputManager.ReleaseKey(Key.A);
            });
            AddAssert("Received the key", () =>
                focusBottomLeft.KeyDownCount == 1 && focusBottomLeft.KeyUpCount == 1 &&
                focusBottomRight.KeyDownCount == 0 && focusBottomRight.KeyUpCount == 1);
            AddStep("Press a joystick (non blocking)", () =>
            {
                InputManager.PressJoystickButton(JoystickButton.Button1);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("Received the joystick button", () =>
                focusBottomLeft.JoystickPressCount == 1 && focusBottomLeft.JoystickReleaseCount == 1 &&
                focusBottomRight.JoystickPressCount == 1 && focusBottomRight.JoystickReleaseCount == 1);
        }

        private void checkFocused(Func<Drawable> d) => AddAssert("check focus", () => d().HasFocus);
        private void checkNotFocused(Func<Drawable> d) => AddAssert("check not focus", () => !d().HasFocus);


        private class FocusOverlay : FocusedOverlayContainer
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

            public override bool ReceiveMouseInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnClick(InputState state)
            {
                if (!box.ReceiveMouseInputAt(state.Mouse.NativeState.Position))
                {
                    State = Visibility.Hidden;
                    return true;
                }

                return base.OnClick(state);
            }

            protected override void OnFocus(InputState state)
            {
                base.OnFocus(state);
                this.FadeTo(1);
            }

            protected override void OnFocusLost(InputState state)
            {
                base.OnFocusLost(state);
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

            protected override bool OnClick(InputState state) => true;

            public override bool AcceptsFocus => true;

            protected override void OnFocus(InputState state)
            {
                base.OnFocus(state);
                Box.FadeTo(1);
            }

            protected override void OnFocusLost(InputState state)
            {
                base.OnFocusLost(state);
                Box.FadeTo(0.5f);
            }

            // only KeyDown is blocking
            protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
            {
                ++KeyDownCount;
                return true;
            }

            protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
            {
                ++KeyUpCount;
                return base.OnKeyUp(state, args);
            }

            protected override bool OnJoystickPress(InputState state, JoystickEventArgs args)
            {
                ++JoystickPressCount;
                return base.OnJoystickPress(state, args);
            }

            protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args)
            {
                ++JoystickReleaseCount;
                return base.OnJoystickRelease(state, args);
            }
        }
    }
}
