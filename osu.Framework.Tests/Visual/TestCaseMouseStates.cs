// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using MouseEventArgs = osu.Framework.Input.MouseEventArgs;
using MouseState = osu.Framework.Input.MouseState;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseMouseStates : TestCase
    {
        private readonly Box marginBox;
        private readonly ManualInputManager manual;
        private readonly FrameworkActionContainer actionContainer;

        private readonly StateTracker s1, s2;

        public TestCaseMouseStates()
        {
            Children = new Drawable[]
            {
                manual = new ManualInputManager
                {
                    FillMode = FillMode.Fit,
                    FillAspectRatio = 1,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.7f),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(1, 1, 1, 0.2f),
                        },
                        actionContainer = new FrameworkActionContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.7f),
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
                                s2 = new StateTracker(2),
                            }
                        },
                        s1 = new StateTracker(1),
                    }
                },
                new StateTracker(0)
            };

            AddStep("return input", () => manual.UseParentState = true);

            // TODO: blocking event testing
        }

        [SetUp]
        public void SetUp()
        {
            // grab manual input control
            manual.AddStates(new InputState { Mouse = new MouseState() });

            s1.Reset();
            s2.Reset();
        }

        [Test]
        public void BasicWheel()
        {
            eventCounts.Clear();

            AddStep("move to centre", () => manual.MoveMouseTo(actionContainer));
            checkEventCount("Move", 1);

            AddStep("scroll some", () => manual.ScrollBy(1));
            checkEventCount("Move");
            checkEventCount("Wheel", 1);
            checkLastWheelDelta(1);

            AddStep("scroll some", () => manual.ScrollBy(-1));
            checkEventCount("Wheel", 1);
            checkLastWheelDelta(-1);
        }

        [Test]
        public void BasicMovement()
        {
            eventCounts.Clear();

            AddStep("push move state", () => manual.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopLeft));
            checkEventCount("Move", 1);
            checkEventCount("Wheel");

            AddStep("push move state", () => manual.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.TopRight));
            checkEventCount("Move", 1);
            checkEventCount("Wheel");
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Width);

            AddStep("push move state", () => manual.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomRight));
            checkEventCount("Move", 1);
            checkLastPositionDelta(() => marginBox.ScreenSpaceDrawQuad.Height);

            AddStep("push two move states", () => manual.AddStates(
                new InputState { Mouse = new MouseState { Position = marginBox.ScreenSpaceDrawQuad.TopLeft } },
                new InputState { Mouse = new MouseState { Position = marginBox.ScreenSpaceDrawQuad.BottomLeft } }
            ));
            checkEventCount("Move", 2);
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.TopLeft, marginBox.ScreenSpaceDrawQuad.BottomLeft));
        }

        [Test]
        public void BasicButtons()
        {
            eventCounts.Clear();
            AddStep("move to centre", () => manual.MoveMouseTo(actionContainer));
            checkEventCount("Move", 1);

            AddStep("press left button", () => manual.PressButton(MouseButton.Left));
            checkEventCount("MouseDown", 1);

            AddStep("press right button", () => manual.PressButton(MouseButton.Right));
            checkEventCount("MouseDown", 1);

            AddStep("release left button", () => manual.ReleaseButton(MouseButton.Left));
            checkEventCount("MouseUp", 1);

            AddStep("release right button", () => manual.ReleaseButton(MouseButton.Right));
            checkEventCount("MouseUp", 1);

            AddStep("press three buttons", () =>
            {
                var state = manual.CurrentState.Clone();
                state.Mouse.SetPressed(MouseButton.Left, true);
                state.Mouse.SetPressed(MouseButton.Right, true);
                state.Mouse.SetPressed(MouseButton.Button1, true);
                manual.AddStates(state);
            });
            checkEventCount("MouseDown", 3);

            AddStep("push empty mouse state", () => manual.AddStates(new InputState { Mouse = new MouseState() }));
            checkEventCount("MouseUp", 3);

            AddStep("move to centre", () => manual.MoveMouseTo(actionContainer));
            checkEventCount("Move", 1);

            AddStep("press two buttons two states", () =>
            {
                var state = manual.CurrentState.Clone();
                state.Mouse.SetPressed(MouseButton.Left, true);
                manual.AddStates(state);
                state = manual.CurrentState.Clone();
                state.Mouse.SetPressed(MouseButton.Right, true);
                manual.AddStates(state);
            });

            checkEventCount("MouseDown", 2);
            checkEventCount("MouseUp", 1);

            AddStep("release", () => manual.AddStates(
                new InputState { Mouse = new MouseState { Position = manual.CurrentState.Mouse.Position } }));

            checkEventCount("Move");
            checkEventCount("MouseUp", 1);
        }

        [Test]
        public void Drag()
        {
            eventCounts.Clear();

            AddStep("move to centre", () => manual.MoveMouseTo(actionContainer));

            AddStep("press left button", () => manual.PressButton(MouseButton.Left));
            checkEventCount("MouseDown", 1);

            AddStep("move bottom left", () => manual.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount("DragStart", 1);

            AddStep("release left button", () => manual.ReleaseButton(MouseButton.Left));
            checkEventCount("MouseUp", 1);
        }

        [Test]
        public void CombinationChanges()
        {
            eventCounts.Clear();

            AddStep("push move state", () => manual.MoveMouseTo(marginBox.ScreenSpaceDrawQuad.BottomLeft));
            checkEventCount("Move", 1);

            AddStep("push move wheel state", () => manual.AddStates(
                new InputState { Mouse = new MouseState { Position = marginBox.ScreenSpaceDrawQuad.Centre, Wheel = 2 } }
            ));
            checkEventCount("Move", 1);
            checkEventCount("Wheel", 1);
            checkLastWheelDelta(2);
            checkLastPositionDelta(() => Vector2.Distance(marginBox.ScreenSpaceDrawQuad.BottomLeft, marginBox.ScreenSpaceDrawQuad.Centre));

            AddStep("push empty state", () => manual.AddStates(
                new InputState()
            ));

            checkEventCount("Move");
            checkEventCount("Wheel");

            AddStep("push empty mouse state", () => manual.AddStates(new InputState { Mouse = new MouseState() }));

            // outside the bounds so should not increment.
            checkEventCount("Move");
            checkEventCount("Wheel");
        }

        private readonly Dictionary<string, int> eventCounts = new Dictionary<string, int>();

        private void checkEventCount(string type, int change = 0)
        {
            eventCounts.TryGetValue(type, out var count);

            count += change;

            AddAssert($"{type} event count {count}", () => s1.CounterFor(type).Count == count && s2.CounterFor(type).Count == count);
            eventCounts[type] = count;
        }

        private void checkLastPositionDelta(Func<float> expected) => AddAssert("correct position delta", () =>
            s1.CounterFor("Move").LastState.Mouse.NativeState.Delta.Length == expected() &&
            s2.CounterFor("Move").LastState.Mouse.NativeState.Delta.Length == expected());

        private void checkLastWheelDelta(int expected) => AddAssert("correct wheel delta", () =>
            s1.CounterFor("Wheel").LastState.Mouse.WheelDelta == expected &&
            s2.CounterFor("Wheel").LastState.Mouse.WheelDelta == expected);

        public class StateTracker : Container
        {
            private readonly SpriteText keyboard;
            private readonly SpriteText mouse;
            private readonly SpriteText source;

            public EventCounter CounterFor(string type) => counterLookup[type];

            private readonly Dictionary<string, EventCounter> counterLookup = new Dictionary<string, EventCounter>();

            public StateTracker(int number)
            {
                RelativeSizeAxes = Axes.Both;
                Margin = new MarginPadding(5);
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            source = new SmallText(),
                            keyboard = new SmallText(),
                            mouse = new SmallText(),
                            addCounter(new EventCounter("Wheel")),
                            addCounter(new EventCounter("Move")),
                            addCounter(new EventCounter("DragStart")),
                            addCounter(new EventCounter("MouseDown")),
                            addCounter(new EventCounter("MouseUp"))
                        }
                    },
                    new BoundedCursorContainer(number)
                };
            }

            protected override bool OnWheel(InputState state) => CounterFor("Wheel").NewState(state);
            protected override bool OnMouseMove(InputState state) => CounterFor("Move").NewState(state);
            protected override bool OnDragStart(InputState state) => CounterFor("DragStart").NewState(state);

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => CounterFor("MouseDown").NewState(state, args);
            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args) => CounterFor("MouseUp").NewState(state, args);

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

                var state = GetContainingInputManager().CurrentState;

                source.Text = GetContainingInputManager().ToString();
                keyboard.Text = state.Keyboard.ToString();
                mouse.Text = state.Mouse.ToString();
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
                    LastState = state;
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

                protected override bool OnWheel(InputState state)
                {
                    circle.MoveToY(circle.Y - state.Mouse.WheelDelta * 10).MoveToY(0, 500, Easing.OutQuint);
                    return base.OnWheel(state);
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
                    circle.FadeColour(state.Mouse.HasAnyButtonPressed ? Color4.Green.Lighten((state.Mouse.Buttons.Count - 1) * 0.3f) : Color4.White, 50);
                }
            }
        }
    }
}
