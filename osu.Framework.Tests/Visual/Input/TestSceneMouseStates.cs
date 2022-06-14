// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneMouseStates : ManualInputManagerTestScene
    {
        private readonly Box marginBox, outerMarginBox;
        private readonly Container actionContainer;

        private readonly StateTracker s1, s2;

        public TestSceneMouseStates()
        {
            Child = new Container
            {
                FillMode = FillMode.Fit,
                FillAspectRatio = 1,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.75f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(1, 1, 1, 0.2f),
                    },
                    s1 = new StateTracker(1),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            outerMarginBox = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(0.9f),
                                Colour = Color4.SkyBlue.Opacity(0.1f),
                            },
                            actionContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.6f),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(1, 1, 1, 0.2f),
                                    },
                                    marginBox = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.8f),
                                        Colour = Color4.SkyBlue.Opacity(0.1f),
                                    },
                                    s2 = new DraggableStateTracker(2),
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            ((Container)InputManager.Parent).Add(new StateTracker(0));
        }

        private void initTestScene()
        {
            eventCounts1.Clear();
            eventCounts2.Clear();
            // InitialMousePosition cannot be used here because the event counters should be resetted after the initial mouse move.
            AddStep("move mouse to center", () => InputManager.MoveMouseTo(actionContainer));
            AddStep("reset event counters", () =>
            {
                s1.Reset();
                s2.Reset();
            });
        }

        private static readonly Type move = typeof(MouseMoveEvent);
        private static readonly Type scroll = typeof(ScrollEvent);
        private static readonly Type mouse_down = typeof(MouseDownEvent);
        private static readonly Type mouse_up = typeof(MouseUpEvent);
        private static readonly Type drag_start = typeof(DragStartEvent);
        private static readonly Type drag = typeof(DragEvent);
        private static readonly Type drag_end = typeof(DragEndEvent);
        private static readonly Type click = typeof(ClickEvent);
        private static readonly Type double_click = typeof(DoubleClickEvent);

        [Test]
        public void BasicScroll()
        {
            initTestScene();

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(-1, 1)));
            checkEventCount(move);
            checkEventCount(scroll, 1);
            checkLastScrollDelta(new Vector2(-1, 1));

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(1, -1)));
            checkEventCount(scroll, 1);
            checkLastScrollDelta(new Vector2(1, -1));
        }

        [Test]
        public void BasicMovement()
        {
            initTestScene();

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            checkEventCount(move, 1);
            checkEventCount(scroll);

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopRight));
            checkEventCount(move, 1);
            checkEventCount(scroll);
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Width);

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount(move, 1);
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Height);

            AddStep("push two moves", () =>
            {
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft);
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft);
            });
            checkEventCount(move, 2);
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.TopLeft, marginBox.ScreenSpaceDrawQuad.BottomLeft));
        }

        [Test]
        public void BasicButtons()
        {
            initTestScene();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(mouse_down, 1);

            AddStep("press right button", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount(mouse_down, 1);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(mouse_up, 1);

            AddStep("release right button", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount(mouse_up, 1);

            AddStep("press three buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
                InputManager.PressButton(MouseButton.Button1);
            });
            checkEventCount(mouse_down, 3);

            AddStep("Release mouse buttons", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Right);
                InputManager.ReleaseButton(MouseButton.Button1);
            });
            checkEventCount(mouse_up, 3);

            AddStep("press two buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
            });

            checkEventCount(mouse_down, 2);
            checkEventCount(mouse_up, 1);

            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Right));

            checkEventCount(move);
            checkEventCount(mouse_up, 1);
        }

        [Test]
        public void Drag()
        {
            initTestScene();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(mouse_down, 1);
            checkIsDragged(false);

            AddStep("move bottom left", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount(drag_start, 1);
            checkEventCount(drag, 1);
            checkIsDragged(true);

            AddStep("move bottom right", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount(drag_start, 0);
            checkEventCount(drag, 1);
            checkIsDragged(true);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(mouse_up, 1);
            checkIsDragged(false);
        }

        [Test]
        public void CombinationChanges()
        {
            initTestScene();

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount(move, 1);

            AddStep("push move and scroll", () =>
            {
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.Centre);
                InputManager.ScrollBy(new Vector2(1, 2));
            });

            checkEventCount(move, 1);
            checkEventCount(scroll, 1);
            checkLastScrollDelta(new Vector2(1, 2));
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.BottomLeft, marginBox.ScreenSpaceDrawQuad.Centre));

            AddStep("Move mouse to out of bounds", () => InputManager.MoveMouseTo(Vector2.Zero));

            checkEventCount(move);
            checkEventCount(scroll);

            AddStep("Move mouse", () =>
            {
                InputManager.MoveMouseTo(new Vector2(10));
                InputManager.ScrollBy(new Vector2(10));
            });

            // outside the bounds so should not increment.
            checkEventCount(move);
            checkEventCount(scroll);
        }

        [Test]
        public void DragAndClick()
        {
            initTestScene();

            // mouseDown on a non-draggable -> mouseUp on a distant position: drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount(drag_start);
            AddStep("drag non-draggable", () => InputManager.MoveMouseTo(marginBox));
            checkEventCount(drag_start, 1, true);
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(click, 1, true);
            checkEventCount(drag_end);

            // mouseDown on a draggable -> mouseUp on the original position: no drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount(drag_start, 1);
            checkIsDragged(true);
            AddStep("return mouse position", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            checkIsDragged(true);
            checkEventCount(drag_end);
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(click);
            checkEventCount(drag_end, 1);
            checkIsDragged(false);

            // mouseDown on a draggable -> mouseUp on a distant position: no drag-clicking
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(drag_start, 1);
            checkEventCount(drag_end, 1);
            checkEventCount(click);
        }

        [Test]
        public void ClickAndDoubleClick()
        {
            initTestScene();

            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(click, 1);
            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(click, 1);
            waitDoubleClickTime();
            AddStep("double click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount(click, 1);
            checkEventCount(double_click, 1);
            waitDoubleClickTime();
            AddStep("triple click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount(click, 2);
            checkEventCount(double_click, 1);

            waitDoubleClickTime();
            AddStep("click then mouse down", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            checkEventCount(click, 1);
            checkEventCount(double_click, 1);
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount(click);
            checkEventCount(double_click);

            waitDoubleClickTime();
            AddStep("double click drag", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.TopLeft);
            });
            checkEventCount(click, 1);
            checkEventCount(double_click, 1);
            checkEventCount(drag_start, 1);
        }

        [Test]
        public void SeparateMouseDown()
        {
            initTestScene();

            AddStep("right down", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount(mouse_down, 1);
            AddStep("move away", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("left click", () => InputManager.Click(MouseButton.Left));
            checkEventCount(mouse_down, 1, true);
            checkEventCount(mouse_up, 1, true);
            AddStep("right up", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount(mouse_up, 1);
        }

        private void waitDoubleClickTime()
        {
            AddWaitStep("wait to don't double click", 2);
        }

        private readonly Dictionary<Type, int> eventCounts1 = new Dictionary<Type, int>(),
                                               eventCounts2 = new Dictionary<Type, int>();

        private void checkEventCount(Type type, int change = 0, bool outer = false)
        {
            eventCounts1.TryGetValue(type, out int count1);
            eventCounts2.TryGetValue(type, out int count2);

            if (outer)
            {
                count1 += change;
            }
            else
            {
                // those types are handled by state tracker 2
                if (!new[] { drag_start, drag, drag_end, click, double_click }.Contains(type))
                    count1 += change;
                count2 += change;
            }

            AddAssert($"{type.Name} count {count1}, {count2}", () => s1.CounterFor(type).Count == count1 && s2.CounterFor(type).Count == count2);

            eventCounts1[type] = count1;
            eventCounts2[type] = count2;
        }

        private void checkLastPositionDelta(Func<float> expected) => AddAssert("correct position delta", () =>
            Precision.AlmostEquals(s1.LastDelta.Length, expected()) &&
            Precision.AlmostEquals(s2.LastDelta.Length, expected()));

        private void checkLastScrollDelta(Vector2 expected) => AddAssert("correct scroll delta", () =>
            Precision.AlmostEquals(s1.LastScrollDelta, expected) &&
            Precision.AlmostEquals(s2.LastScrollDelta, expected));

        private void checkIsDragged(bool isDragged) => AddAssert(isDragged ? "dragged" : "not dragged", () => s2.IsDragged == isDragged);

        public class StateTracker : Container
        {
            private readonly SpriteText keyboard;
            private readonly SpriteText mouse;
            private readonly SpriteText source;
            protected readonly FillFlowContainer TextContainer;

            public EventCounter CounterFor(Type type) => counterLookup[type];
            public Vector2 LastDelta { get; private set; }
            public Vector2 LastScrollDelta { get; private set; }

            private readonly Dictionary<Type, EventCounter> counterLookup = new Dictionary<Type, EventCounter>();

            public StateTracker(int number)
            {
                RelativeSizeAxes = Axes.Both;
                Margin = new MarginPadding(5);
                Children = new Drawable[]
                {
                    TextContainer = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            source = new SmallText(),
                            keyboard = new SmallText(),
                            mouse = new SmallText(),
                            addCounter(typeof(ScrollEvent)),
                            addCounter(typeof(MouseMoveEvent)),
                            addCounter(typeof(DragStartEvent)),
                            addCounter(typeof(DragEvent)),
                            addCounter(typeof(DragEndEvent)),
                            addCounter(typeof(MouseDownEvent)),
                            addCounter(typeof(MouseUpEvent)),
                            addCounter(typeof(ClickEvent)),
                            addCounter(typeof(DoubleClickEvent))
                        }
                    },
                    new BoundedCursorContainer(number)
                };
            }

            protected override bool Handle(UIEvent e)
            {
                var type = e.GetType();
                if (!counterLookup.TryGetValue(type, out var counter))
                    return base.Handle(e);

                ++counter.Count;

                switch (e)
                {
                    case MouseMoveEvent mouseMove:
                        LastDelta = mouseMove.ScreenSpaceMousePosition - mouseMove.ScreenSpaceLastMousePosition;
                        break;

                    case ScrollEvent scroll:
                        LastScrollDelta = scroll.ScrollDelta;
                        break;
                }

                return type == click || type == double_click;
            }

            private EventCounter addCounter(Type type)
            {
                var counter = new EventCounter(type);
                counterLookup.Add(type, counter);
                return counter;
            }

            public void Reset()
            {
                foreach (var kvp in counterLookup)
                    kvp.Value.Reset();

                LastDelta = Vector2.Zero;
                LastScrollDelta = Vector2.Zero;
            }

            protected override void Update()
            {
                base.Update();

                var inputManager = GetContainingInputManager();

                if (inputManager != null)
                {
                    var state = inputManager.CurrentState;

                    source.Text = inputManager.ToString();
                    keyboard.Text = state.Keyboard.ToString();
                    mouse.Text = state.Mouse.ToString();
                }
            }

            public class SmallText : SpriteText
            {
                public SmallText()
                {
                    Font = new FontUsage(size: 14);
                }
            }

            public class EventCounter : CompositeDrawable
            {
                private int count;
                private readonly SpriteText text;

                public EventCounter(Type eventType)
                {
                    AutoSizeAxes = Axes.Both;
                    InternalChild = text = new SmallText();

                    Name = eventType.Name.Replace("Event", "");

                    Reset();
                }

                public int Count
                {
                    get => count;
                    set
                    {
                        count = value;
                        text.Text = $"{Name}: {Count}";
                    }
                }

                public void Reset()
                {
                    Count = 0;
                }
            }

            public class BoundedCursorContainer : Container
            {
                private readonly Circle circle;

                public BoundedCursorContainer(int number)
                {
                    RelativeSizeAxes = Axes.Both;

                    Child = new Container
                    {
                        Size = new Vector2(5),
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            new Circle
                            {
                                Origin = Anchor.Centre,
                                Size = new Vector2(40),
                                Alpha = 0.1f,
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Origin = Anchor.Centre,
                                X = -5 + number * 5,
                                Y = -8,
                                Child = circle = new Circle
                                {
                                    RelativeSizeAxes = Axes.Both,
                                }
                            }
                        }
                    };
                }

                protected override bool OnMouseMove(MouseMoveEvent e)
                {
                    Child.MoveTo(e.MousePosition, 100, Easing.OutQuint);
                    return base.OnMouseMove(e);
                }

                protected override bool OnScroll(ScrollEvent e)
                {
                    circle.MoveTo(circle.Position - e.ScrollDelta * 10).MoveTo(Vector2.Zero, 500, Easing.OutQuint);
                    return base.OnScroll(e);
                }

                protected override bool OnMouseDown(MouseDownEvent e)
                {
                    adjustForMouseDown(e);
                    return base.OnMouseDown(e);
                }

                protected override void OnMouseUp(MouseUpEvent e)
                {
                    adjustForMouseDown(e);
                    base.OnMouseUp(e);
                }

                private void adjustForMouseDown(MouseEvent e)
                {
                    circle.FadeColour(e.HasAnyButtonPressed ? Color4.Green.Lighten((e.PressedButtons.Count() - 1) * 0.3f) : Color4.White, 50);
                }
            }
        }

        public class DraggableStateTracker : StateTracker
        {
            private readonly SmallText dragStatus;

            public DraggableStateTracker(int number)
                : base(number)
            {
                TextContainer.Add(dragStatus = new SmallText());
            }

            protected override bool Handle(UIEvent e) => base.Handle(e) || e is DragStartEvent;

            protected override void Update()
            {
                base.Update();
                dragStatus.Text = $"IsDragged = {IsDragged}";
            }
        }
    }
}
