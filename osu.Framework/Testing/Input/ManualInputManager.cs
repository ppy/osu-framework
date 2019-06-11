// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualInputManager : PassThroughInputManager
    {
        private readonly ManualInputHandler handler;

        protected override Container<Drawable> Content => content;

        private readonly Container content;

        public ManualInputManager()
        {
            UseParentInput = true;
            AddHandler(handler = new ManualInputHandler());

            InternalChildren = new Drawable[]
            {
                content = new Container { RelativeSizeAxes = Axes.Both },
                new TestCursorContainer(),
            };
        }

        public void Input(IInput input)
        {
            UseParentInput = false;
            handler.EnqueueInput(input);
        }

        public void PressKey(Key key) => Input(new KeyboardKeyInput(key, true));
        public void ReleaseKey(Key key) => Input(new KeyboardKeyInput(key, false));

        public void ScrollBy(Vector2 delta, bool isPrecise = false) => Input(new MouseScrollRelativeInput { Delta = delta, IsPrecise = isPrecise });
        public void ScrollHorizontalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(delta, 0), isPrecise);
        public void ScrollVerticalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(0, delta), isPrecise);

        public void MoveMouseTo(Drawable drawable) => MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
        public void MoveMouseTo(Vector2 position) => Input(new MousePositionAbsoluteInput { Position = position });

        public void Click(MouseButton button)
        {
            PressButton(button);
            ReleaseButton(button);
        }

        public void PressButton(MouseButton button) => Input(new MouseButtonInput(button, true));
        public void ReleaseButton(MouseButton button) => Input(new MouseButtonInput(button, false));

        public void PressJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, true));
        public void ReleaseJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, false));

        private class ManualInputHandler : InputHandler
        {
            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;

            public void EnqueueInput(IInput input)
            {
                PendingInputs.Enqueue(input);
            }
        }

        private class TestCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new TestCursor();

            private class TestCursor : CompositeDrawable
            {
                private readonly Container circle;

                private readonly Container border;
                private readonly Container left;
                private readonly Container right;

                public TestCursor()
                {
                    Size = new Vector2(30);

                    Origin = Anchor.Centre;

                    InternalChildren = new Drawable[]
                    {
                        left = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Alpha = 0,
                            Width = 0.5f,
                            Child = new CircularContainer
                            {
                                Size = new Vector2(30),
                                Masking = true,
                                BorderThickness = 5,
                                BorderColour = Color4.Cyan,
                                Child = new Box
                                {
                                    Colour = Color4.Black,
                                    Alpha = 0.1f,
                                    RelativeSizeAxes = Axes.Both,
                                },
                            },
                        },
                        right = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Alpha = 0,
                            Width = 0.5f,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Child = new CircularContainer
                            {
                                Size = new Vector2(30),
                                X = -15,
                                Masking = true,
                                BorderThickness = 5,
                                BorderColour = Color4.Cyan,
                                Child = new Box
                                {
                                    Colour = Color4.Black,
                                    Alpha = 0.1f,
                                    RelativeSizeAxes = Axes.Both,
                                },
                            },
                        },
                        border = new CircularContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            BorderThickness = 2,
                            BorderColour = Color4.Cyan,
                            Child = new Box
                            {
                                Colour = Color4.Black,
                                Alpha = 0.1f,
                                RelativeSizeAxes = Axes.Both,
                            },
                        },
                        circle = new CircularContainer
                        {
                            Size = new Vector2(8),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            BorderThickness = 2,
                            BorderColour = Color4.White,
                            Child = new Box
                            {
                                Colour = Color4.Red,
                                RelativeSizeAxes = Axes.Both,
                            },
                        },
                    };
                }

                protected override bool OnMouseDown(MouseDownEvent e)
                {
                    switch (e.Button)
                    {
                        case MouseButton.Left:
                            left.FadeIn();
                            break;

                        case MouseButton.Right:
                            right.FadeIn();
                            break;
                    }

                    updateBorder(e);
                    return base.OnMouseDown(e);
                }

                protected override bool OnMouseUp(MouseUpEvent e)
                {
                    switch (e.Button)
                    {
                        case MouseButton.Left:
                            left.FadeOut(500);
                            break;

                        case MouseButton.Right:
                            right.FadeOut(500);
                            break;
                    }

                    updateBorder(e);
                    return base.OnMouseUp(e);
                }

                protected override bool OnScroll(ScrollEvent e)
                {
                    circle.MoveTo(circle.Position - e.ScrollDelta * 10).MoveTo(Vector2.Zero, 500, Easing.OutQuint);
                    return base.OnScroll(e);
                }

                private void updateBorder(MouseButtonEvent e)
                {
                    border.BorderColour = e.CurrentState.Mouse.Buttons.Any() ? Color4.Red : Color4.Cyan;
                }
            }
        }
    }
}
