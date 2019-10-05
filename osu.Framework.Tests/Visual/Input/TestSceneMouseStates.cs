// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneMouseStates : TestScene
    {
        public readonly Box MarginBox, OuterMarginBox;
        public readonly Container ActionContainer;

        public readonly StateTracker S1, S2;

        public readonly Dictionary<Type, int> EventCounts1 = new Dictionary<Type, int>(),
                                              EventCounts2 = new Dictionary<Type, int>();

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
                    S1 = new StateTracker(1),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            OuterMarginBox = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(0.9f),
                                Colour = Color4.SkyBlue.Opacity(0.1f),
                            },
                            ActionContainer = new Container
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
                                    MarginBox = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.8f),
                                        Colour = Color4.SkyBlue.Opacity(0.1f),
                                    },
                                    S2 = new DraggableStateTracker(2),
                                }
                            }
                        }
                    }
                }
            };
        }

        public static readonly Type MOVE = typeof(MouseMoveEvent);
        public static readonly Type SCROLL = typeof(ScrollEvent);
        public static readonly Type MOUSE_DOWN = typeof(MouseDownEvent);
        public static readonly Type MOUSE_UP = typeof(MouseUpEvent);
        public static readonly Type DRAG_START = typeof(DragStartEvent);
        public static readonly Type DRAG_END = typeof(DragEndEvent);
        public static readonly Type CLICK = typeof(ClickEvent);
        public static readonly Type DOUBLE_CLICK = typeof(DoubleClickEvent);

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

                return type == CLICK || type == DOUBLE_CLICK;
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

                protected override bool OnMouseUp(MouseUpEvent e)
                {
                    adjustForMouseDown(e);
                    return base.OnMouseUp(e);
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
