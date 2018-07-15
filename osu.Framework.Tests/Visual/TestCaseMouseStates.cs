// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using MouseEventArgs = osu.Framework.Input.MouseEventArgs;
using osu.Framework.MathUtils;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseMouseStates : ManualInputManagerTestCase
    {
        private readonly Box marginBox, outerMarginBox;
        private readonly FrameworkActionContainer actionContainer;

        private readonly StateTracker s1, s2;

        public TestCaseMouseStates()
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
                            actionContainer = new FrameworkActionContainer
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

        private void initTestCase()
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

        [Test]
        public void BasicScroll()
        {
            initTestCase();

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(-1, 1)));
            checkEventCount("Move");
            checkEventCount("Scroll", 1);
            checkLastScrollDelta(new Vector2(-1, 1));

            AddStep("scroll some", () => InputManager.ScrollBy(new Vector2(1, -1)));
            checkEventCount("Scroll", 1);
            checkLastScrollDelta(new Vector2(1, -1));
        }

        [Test]
        public void BasicMovement()
        {
            initTestCase();

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            checkEventCount("Move", 1);
            checkEventCount("Scroll");

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopRight));
            checkEventCount("Move", 1);
            checkEventCount("Scroll");
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Width);

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount("Move", 1);
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Height);

            AddStep("push two moves", () =>
            {
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft);
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft);
            });
            checkEventCount("Move", 2);
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.TopLeft, marginBox.ScreenSpaceDrawQuad.BottomLeft));
        }

        [Test]
        public void BasicButtons()
        {
            initTestCase();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount("MouseDown", 1);

            AddStep("press right button", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount("MouseDown", 1);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount("MouseUp", 1);

            AddStep("release right button", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount("MouseUp", 1);

            AddStep("press three buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
                InputManager.PressButton(MouseButton.Button1);
            });
            checkEventCount("MouseDown", 3);

            AddStep("Release mouse buttons", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Right);
                InputManager.ReleaseButton(MouseButton.Button1);
            });
            checkEventCount("MouseUp", 3);

            AddStep("press two buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Right);
            });

            checkEventCount("MouseDown", 2);
            checkEventCount("MouseUp", 1);

            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Right));

            checkEventCount("Move");
            checkEventCount("MouseUp", 1);
        }

        [Test]
        public void Drag()
        {
            initTestCase();

            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount("MouseDown", 1);
            checkIsDragged(false);

            AddStep("move bottom left", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount("DragStart", 1);
            checkIsDragged(true);

            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount("MouseUp", 1);
            checkIsDragged(false);
        }

        [Test]
        public void CombinationChanges()
        {
            initTestCase();

            AddStep("push move", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount("Move", 1);

            AddStep("push move and scroll", () =>
            {
                InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.Centre);
                InputManager.ScrollBy(new Vector2(1, 2));
            });

            checkEventCount("Move", 1);
            checkEventCount("Scroll", 1);
            checkLastScrollDelta(new Vector2(1, 2));
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.BottomLeft, marginBox.ScreenSpaceDrawQuad.Centre));

            AddStep("Move mouse to out of bounds", () => InputManager.MoveMouseTo(Vector2.Zero));

            checkEventCount("Move");
            checkEventCount("Scroll");

            AddStep("Move mouse", () =>
            {
                InputManager.MoveMouseTo(new Vector2(10));
                InputManager.ScrollBy(new Vector2(10));
            });

            // outside the bounds so should not increment.
            checkEventCount("Move");
            checkEventCount("Scroll");
        }

        [Test]
        public void DragAndClick()
        {
            initTestCase();

            // mouseDown on a non-draggable -> mouseUp on a distant position: drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            checkEventCount("DragStart");
            AddStep("drag non-draggable", () => InputManager.MoveMouseTo(marginBox));
            checkEventCount("DragStart", 1, true);
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount("Click", 1, true);
            checkEventCount("DragEnd");

            // mouseDown on a draggable -> mouseUp on the original position: no drag-clicking
            AddStep("move mouse", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount("DragStart", 1);
            checkIsDragged(true);
            AddStep("return mouse position", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            checkIsDragged(true);
            checkEventCount("DragEnd");
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount("Click");
            checkEventCount("DragEnd", 1);
            checkIsDragged(false);

            // mouseDown on a draggable -> mouseUp on a distant position: no drag-clicking
            AddStep("press left button", () => InputManager.PressButton(MouseButton.Left));
            AddStep("drag draggable", () => InputManager.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            AddStep("release left button", () => InputManager.ReleaseButton(MouseButton.Left));
            checkEventCount("DragStart", 1);
            checkEventCount("DragEnd", 1);
            checkEventCount("Click");
        }

        [Test]
        public void ClickAndDoubleClick()
        {
            initTestCase();

            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount("Click", 1);
            waitDoubleClickTime();
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkEventCount("Click", 1);
            waitDoubleClickTime();
            AddStep("double click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount("Click", 1);
            checkEventCount("DoubleClick", 1);
            waitDoubleClickTime();
            AddStep("triple click", () =>
            {
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            checkEventCount("Click", 2);
            checkEventCount("DoubleClick", 1);
        }

        [Test]
        public void SeparateMouseDown()
        {
            initTestCase();

            AddStep("right down", () => InputManager.PressButton(MouseButton.Right));
            checkEventCount("MouseDown", 1);
            AddStep("move away", () => InputManager.MoveMouseTo(outerMarginBox.ScreenSpaceDrawQuad.TopLeft));
            AddStep("left click", () => InputManager.Click(MouseButton.Left));
            checkEventCount("MouseDown", 1, true);
            checkEventCount("MouseUp", 1, true);
            AddStep("right up", () => InputManager.ReleaseButton(MouseButton.Right));
            checkEventCount("MouseUp", 1);
        }

        private void waitDoubleClickTime()
        {
            AddWaitStep(2, "wait to don't double click");
        }

        private readonly Dictionary<string, int> eventCounts1 = new Dictionary<string, int>(),
                                                 eventCounts2 = new Dictionary<string, int>();

        private void checkEventCount(string type, int change = 0, bool outer = false)
        {
            eventCounts1.TryGetValue(type, out var count1);
            eventCounts2.TryGetValue(type, out var count2);

            if (outer)
            {
                count1 += change;
            }
            else
            {
                // those types are handled by state tracker 2
                if (type != "Click" && type != "DoubleClick" && type != "DragStart" && type != "DragEnd")
                    count1 += change;
                count2 += change;
            }

            AddAssert($"{type} event count {count1}, {count2}", () => s1.CounterFor(type).Count == count1 && s2.CounterFor(type).Count == count2);

            eventCounts1[type] = count1;
            eventCounts2[type] = count2;
        }

        private void checkLastPositionDelta(Func<float> expected) => AddAssert("correct position delta", () =>
            s1.CounterFor("Move").LastState.Mouse.NativeState.Delta.Length == expected() &&
            s2.CounterFor("Move").LastState.Mouse.NativeState.Delta.Length == expected());

        private void checkLastScrollDelta(Vector2 expected) => AddAssert("correct scroll delta", () =>
            Precision.AlmostEquals(s1.CounterFor("Scroll").LastState.Mouse.ScrollDelta, expected) &&
            Precision.AlmostEquals(s2.CounterFor("Scroll").LastState.Mouse.ScrollDelta, expected));

        private void checkIsDragged(bool isDragged) => AddAssert(isDragged ? "dragged" : "not dragged", () => s2.IsDragged == isDragged);

        public class StateTracker : Container
        {
            private readonly SpriteText keyboard;
            private readonly SpriteText mouse;
            private readonly SpriteText source;
            protected readonly FillFlowContainer TextContainer;

            public EventCounter CounterFor(string type) => counterLookup[type];

            private readonly Dictionary<string, EventCounter> counterLookup = new Dictionary<string, EventCounter>();

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
                            addCounter(new EventCounter("Scroll")),
                            addCounter(new EventCounter("Move")),
                            addCounter(new EventCounter("DragStart")),
                            addCounter(new EventCounter("DragEnd")),
                            addCounter(new EventCounter("MouseDown")),
                            addCounter(new EventCounter("MouseUp")),
                            addCounter(new EventCounter("Click")),
                            addCounter(new EventCounter("DoubleClick"))
                        }
                    },
                    new BoundedCursorContainer(number)
                };
            }

            protected override bool OnScroll(InputState state) => CounterFor("Scroll").NewState(state);
            protected override bool OnMouseMove(InputState state) => CounterFor("Move").NewState(state);
            protected override bool OnDragStart(InputState state) => CounterFor("DragStart").NewState(state);
            protected override bool OnDragEnd(InputState state) => CounterFor("DragEnd").NewState(state);

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => CounterFor("MouseDown").NewState(state, args);
            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args) => CounterFor("MouseUp").NewState(state, args);

            protected override bool OnClick(InputState state)
            {
                CounterFor("Click").NewState(state);
                return true;
            }

            protected override bool OnDoubleClick(InputState state)
            {
                CounterFor("DoubleClick").NewState(state);
                return true;
            }

            private EventCounter addCounter(EventCounter counter)
            {
                counterLookup.Add(counter.Name, counter);
                return counter;
            }

            public void Reset()
            {
                foreach (var kvp in counterLookup)
                    kvp.Value.Reset();
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
                    TextSize = 14;
                }
            }

            public class EventCounter : CompositeDrawable
            {
                public InputState LastState;
                public MouseEventArgs LastArgs;

                private int count;
                private readonly SpriteText text;

                public EventCounter(string name)
                {
                    AutoSizeAxes = Axes.Both;

                    InternalChild = text = new SmallText();
                    Name = name;
                    Reset();
                }

                public int Count
                {
                    get { return count; }
                    set
                    {
                        count = value;
                        text.Text = $"{Name}: {Count}";
                    }
                }

                public void Reset()
                {
                    Count = 0;
                    LastState = null;
                }

                public bool NewState(InputState state, MouseEventArgs args = null)
                {
                    LastState = state.Clone();
                    LastArgs = args;
                    Count++;

                    return false;
                }
            }

            private class BoundedCursorContainer : Container
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

                protected override bool OnMouseMove(InputState state)
                {
                    Child.MoveTo(state.Mouse.Position, 100, Easing.OutQuint);
                    return base.OnMouseMove(state);
                }

                protected override bool OnScroll(InputState state)
                {
                    circle.MoveTo(circle.Position - state.Mouse.ScrollDelta * 10).MoveTo(Vector2.Zero, 500, Easing.OutQuint);
                    return base.OnScroll(state);
                }

                protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
                {
                    adjustForMouseDown(state);
                    return base.OnMouseDown(state, args);
                }

                protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
                {
                    adjustForMouseDown(state);
                    return base.OnMouseUp(state, args);
                }

                private void adjustForMouseDown(InputState state)
                {
                    circle.FadeColour(state.Mouse.HasAnyButtonPressed ? Color4.Green.Lighten((state.Mouse.Buttons.Count() - 1) * 0.3f) : Color4.White, 50);
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

            protected override bool OnDragStart(InputState state)
            {
                base.OnDragStart(state);
                return true;
            }

            protected override void Update()
            {
                base.Update();
                dragStatus.Text = $"IsDragged = {IsDragged}";
            }
        }
    }
}
