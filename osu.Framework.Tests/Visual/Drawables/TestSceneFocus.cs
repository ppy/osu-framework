// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneFocus : ManualInputManagerTestScene
    {
        private FocusOverlay overlay;
        private RequestingFocusBox requestingFocus;

        private FocusBox focusTopLeft;
        private FocusBox focusBottomLeft;
        private FocusBox focusBottomRight;

        public TestSceneFocus()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
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
        });

        [Test]
        public void FocusedOverlayTakesFocusOnShow()
        {
            AddAssert("overlay not visible", () => overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => overlay);

            AddStep("show overlay", () => overlay.Show());
            checkFocused(() => overlay);

            AddStep("hide overlay", () => overlay.Hide());
            checkNotFocused(() => overlay);
        }

        [Test]
        public void FocusedOverlayLosesFocusOnClickAway()
        {
            AddAssert("overlay not visible", () => overlay.State.Value == Visibility.Hidden);
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

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus"/> to a drawable with disabled <see cref="Drawable.AcceptsFocus"/> returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void DisabledFocusDrawableCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("disable focus from top left", () => focusTopLeft.AllowAcceptingFocus = false);
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
        }

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus"/> to a non-present drawable returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void NotPresentDrawableCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("hide top left", () => focusTopLeft.Alpha = 0);
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
        }

        /// <summary>
        /// Ensures that performing <see cref="InputManager.ChangeFocus"/> to a drawable of a non-present parent returns <see langword="false"/>.
        /// </summary>
        [Test]
        public void DrawableOfNotPresentParentCannotReceiveFocusViaChangeFocus()
        {
            checkFocused(() => requestingFocus);

            AddStep("wrap top left in hidden container", () =>
            {
                Container container;

                Add(container = new Container
                {
                    Alpha = 0,
                    RelativeSizeAxes = Axes.Both,
                });

                Remove(focusTopLeft);
                container.Add(focusTopLeft);
            });
            AddAssert("cannot switch focus to top left", () => !InputManager.ChangeFocus(focusTopLeft));

            checkFocused(() => requestingFocus);
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
                focusBottomRight.KeyDownCount == 0 && focusBottomRight.KeyUpCount == 0);
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

            public bool AllowAcceptingFocus = true;

            public override bool AcceptsFocus => AllowAcceptingFocus;

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

            protected override void OnKeyUp(KeyUpEvent e)
            {
                ++KeyUpCount;
                base.OnKeyUp(e);
            }

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                ++JoystickPressCount;
                return base.OnJoystickPress(e);
            }

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
            {
                ++JoystickReleaseCount;
                base.OnJoystickRelease(e);
            }
        }
    }
}
