// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public partial class TestSceneKeyBindingsGrid : ManualInputManagerTestScene
    {
        private readonly KeyBindingTester none, noneExact, noneModifiers, unique, all;

        public TestSceneKeyBindingsGrid()
        {
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[]
                    {
                        none = new KeyBindingTester(SimultaneousBindingMode.None, KeyCombinationMatchingMode.Any),
                        noneExact = new KeyBindingTester(SimultaneousBindingMode.None, KeyCombinationMatchingMode.Exact),
                        noneModifiers = new KeyBindingTester(SimultaneousBindingMode.None, KeyCombinationMatchingMode.Modifiers)
                    },
                    new Drawable[]
                    {
                        unique = new KeyBindingTester(SimultaneousBindingMode.Unique, KeyCombinationMatchingMode.Any),
                        all = new KeyBindingTester(SimultaneousBindingMode.All, KeyCombinationMatchingMode.Any)
                    },
                }
            };
        }

        private readonly List<Key> pressedKeys = new List<Key>();
        private readonly List<MouseButton> pressedMouseButtons = new List<MouseButton>();
        private readonly Dictionary<TestButton, EventCounts> lastEventCounts = new Dictionary<TestButton, EventCounts>();

        private void toggleKey(Key key)
        {
            if (!pressedKeys.Contains(key))
            {
                pressedKeys.Add(key);
                AddStep($"press {key}", () => InputManager.PressKey(key));
            }
            else
            {
                pressedKeys.Remove(key);
                AddStep($"release {key}", () => InputManager.ReleaseKey(key));
            }
        }

        private void toggleMouseButton(MouseButton button)
        {
            if (!pressedMouseButtons.Contains(button))
            {
                pressedMouseButtons.Add(button);
                AddStep($"press {button}", () => InputManager.PressButton(button));
            }
            else
            {
                pressedMouseButtons.Remove(button);
                AddStep($"release {button}", () => InputManager.ReleaseButton(button));
            }
        }

        private void scrollMouseWheel(int dx, int dy)
        {
            if (dx != 0) AddStep($"scroll wheel horizontally {dx}", () => InputManager.ScrollHorizontalBy(dx));
            if (dy != 0) AddStep($"scroll wheel {dy}", () => InputManager.ScrollVerticalBy(dy));
        }

        private void check(TestAction action, params CheckConditions[] entries)
        {
            AddAssert($"check {action}", () =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var entry in entries)
                    {
                        var scrollEntry = entry as ScrollCheckConditions;
                        var testButton = entry.Tester[action];

                        if (!lastEventCounts.TryGetValue(testButton, out var count))
                            lastEventCounts[testButton] = count = new EventCounts();

                        count.OnPressedCount += entry.OnPressedDelta;
                        count.OnReleasedCount += entry.OnReleasedDelta;
                        count.OnScrollCount += scrollEntry?.OnScrollCount ?? 0;

                        Assert.AreEqual(count.OnPressedCount, testButton.OnPressedCount, $"{testButton.Concurrency} {testButton.Action} OnPressedCount");
                        Assert.AreEqual(count.OnReleasedCount, testButton.OnReleasedCount, $"{testButton.Concurrency} {testButton.Action} OnReleasedCount");

                        if (testButton is ScrollTestButton scrollTestButton && scrollEntry != null)
                        {
                            Assert.AreEqual(count.OnScrollCount, scrollTestButton.OnScrollCount, $"{testButton.Concurrency} {testButton.Action} OnScrollCount");
                            Assert.AreEqual(scrollEntry.LastScrollAmount, scrollTestButton.LastScrollAmount, $"{testButton.Concurrency} {testButton.Action} LastScrollAmount");
                        }
                    }
                });
                return true;
            });
        }

        private void checkPressed(TestAction action, int noneDelta, int noneExactDelta, int noneModifiersDelta, int uniqueDelta, int allDelta)
        {
            check(action,
                new CheckConditions(none, noneDelta, 0),
                new CheckConditions(noneExact, noneExactDelta, 0),
                new CheckConditions(noneModifiers, noneModifiersDelta, 0),
                new CheckConditions(unique, uniqueDelta, 0),
                new CheckConditions(all, allDelta, 0));
        }

        private void checkReleased(TestAction action, int noneDelta, int noneExactDelta, int noneModifiersDelta, int uniqueDelta, int allDelta)
        {
            check(action,
                new CheckConditions(none, 0, noneDelta),
                new CheckConditions(noneExact, 0, noneExactDelta),
                new CheckConditions(noneModifiers, 0, noneModifiersDelta),
                new CheckConditions(unique, 0, uniqueDelta),
                new CheckConditions(all, 0, allDelta));
        }

        private void wrapTest(Action inner)
        {
            AddStep("init", () =>
            {
                foreach (var mode in new[] { none, noneExact, noneModifiers, unique, all })
                {
                    foreach (var action in Enum.GetValues(typeof(TestAction)).Cast<TestAction>())
                    {
                        mode[action].Reset();
                    }
                }

                lastEventCounts.Clear();
            });
            pressedKeys.Clear();
            pressedMouseButtons.Clear();
            inner();
            foreach (var key in pressedKeys.ToArray())
                toggleKey(key);
            foreach (var button in pressedMouseButtons.ToArray())
                toggleMouseButton(button);

            foreach (var mode in new[] { none, noneExact, noneModifiers, unique, all })
            {
                foreach (var action in Enum.GetValues(typeof(TestAction)).Cast<TestAction>())
                {
                    var testButton = mode[action];
                    Trace.Assert(testButton.OnPressedCount == testButton.OnReleasedCount);
                    if (testButton is ScrollTestButton scrollTestButton)
                        Trace.Assert(scrollTestButton.OnScrollCount == testButton.OnPressedCount);
                }
            }
        }

        [Test]
        public void SimultaneousBindingModes()
        {
            wrapTest(() =>
            {
                toggleKey(Key.A);
                checkPressed(TestAction.A, 1, 1, 1, 1, 1);
                toggleKey(Key.S);
                checkReleased(TestAction.A, 1, 1, 1, 0, 0);
                checkPressed(TestAction.S, 1, 0, 1, 1, 1);
                toggleKey(Key.A);
                checkReleased(TestAction.A, 0, 0, 0, 1, 1);
                checkPressed(TestAction.S, 0, 0, 0, 0, 0);
                toggleKey(Key.S);
                checkReleased(TestAction.S, 1, 0, 1, 1, 1);

                toggleKey(Key.D);
                checkPressed(TestAction.D_or_F, 1, 1, 1, 1, 1);
                toggleKey(Key.F);
                check(TestAction.D_or_F, new CheckConditions(none, 1, 1), new CheckConditions(noneExact, 0, 1), new CheckConditions(noneModifiers, 1, 1), new CheckConditions(unique, 0, 0), new CheckConditions(all, 1, 0));
                toggleKey(Key.F);
                checkReleased(TestAction.D_or_F, 0, 0, 0, 0, 1);
                toggleKey(Key.D);
                checkReleased(TestAction.D_or_F, 1, 0, 1, 1, 1);

                toggleKey(Key.ShiftLeft);
                toggleKey(Key.AltLeft);
                checkPressed(TestAction.Alt_and_LShift, 1, 1, 1, 1, 1);
                toggleKey(Key.A);
                checkPressed(TestAction.LShift_A, 1, 0, 0, 1, 1);
                toggleKey(Key.AltLeft);
                toggleKey(Key.ShiftLeft);
            });
        }

        [Test]
        public void PerSideModifierKeys()
        {
            wrapTest(() =>
            {
                toggleKey(Key.ShiftLeft);
                checkPressed(TestAction.AnyShift, 1, 1, 1, 1, 1);
                checkPressed(TestAction.LShift, 0, 0, 0, 1, 1);
                checkPressed(TestAction.RShift, 0, 0, 0, 0, 0);

                toggleKey(Key.A);
                checkPressed(TestAction.AnyShift_A, 0, 0, 0, 1, 1);
                checkPressed(TestAction.LShift_A, 1, 1, 1, 1, 1);
                checkReleased(TestAction.AnyShift, 1, 1, 1, 0, 0);

                toggleKey(Key.ShiftRight);
                checkReleased(TestAction.LShift_A, 1, 0, 0, 0, 0);
                checkPressed(TestAction.RShift, 1, 0, 0, 1, 1);
                checkPressed(TestAction.AnyShift, 0, 0, 0, 0, 0);

                toggleKey(Key.ShiftLeft);
                checkReleased(TestAction.LShift, 0, 0, 0, 1, 1);
                checkReleased(TestAction.LShift_A, 0, 1, 1, 1, 1);
                checkReleased(TestAction.AnyShift, 0, 0, 0, 0, 0);

                toggleKey(Key.ShiftRight);
                checkReleased(TestAction.AnyShift, 0, 0, 0, 1, 1);
            });
        }

        [Test]
        public void BothSideModifierKeys()
        {
            wrapTest(() =>
            {
                toggleKey(Key.AltLeft);
                checkPressed(TestAction.Alt, 1, 1, 1, 1, 1);
                toggleKey(Key.A);
                checkReleased(TestAction.Alt, 1, 1, 1, 0, 0);
                checkPressed(TestAction.Alt_A, 1, 1, 1, 1, 1);
                toggleKey(Key.AltRight);
                checkPressed(TestAction.Alt, 0, 0, 0, 0, 0);
                checkReleased(TestAction.Alt_A, 0, 0, 0, 0, 0);
                toggleKey(Key.AltLeft);
                checkReleased(TestAction.Alt, 0, 0, 0, 0, 0);
                checkReleased(TestAction.Alt_A, 0, 0, 0, 0, 0);
                toggleKey(Key.AltRight);
                checkReleased(TestAction.Alt, 0, 0, 0, 1, 1);
                checkReleased(TestAction.Alt_A, 1, 1, 1, 1, 1);
                toggleKey(Key.A);

                toggleKey(Key.ControlLeft);
                toggleKey(Key.AltLeft);
                checkPressed(TestAction.Ctrl_and_Alt, 1, 1, 1, 1, 1);
            });
        }

        [Test]
        public void MouseScrollAndButtons()
        {
            wrapTest(() =>
            {
                var allPressAndReleased = new[]
                {
                    new CheckConditions(none, 1, 1),
                    new CheckConditions(noneExact, 1, 1),
                    new CheckConditions(noneModifiers, 1, 1),
                    new CheckConditions(unique, 1, 1),
                    new CheckConditions(all, 1, 1)
                };

                scrollMouseWheel(0, 1);
                check(TestAction.WheelUp, allPressAndReleased);
                scrollMouseWheel(0, -1);
                check(TestAction.WheelDown, allPressAndReleased);

                scrollMouseWheel(1, 0);
                check(TestAction.WheelLeft, allPressAndReleased);
                scrollMouseWheel(-1, 0);
                check(TestAction.WheelRight, allPressAndReleased);

                toggleKey(Key.ControlLeft);
                scrollMouseWheel(0, 1);
                toggleKey(Key.ControlLeft);
                check(TestAction.Ctrl_and_WheelUp, allPressAndReleased);
                toggleMouseButton(MouseButton.Left);
                toggleMouseButton(MouseButton.Left);
                check(TestAction.LeftMouse, allPressAndReleased);
                toggleMouseButton(MouseButton.Right);
                toggleMouseButton(MouseButton.Right);
                check(TestAction.RightMouse, allPressAndReleased);
            });
        }

        [Test]
        public void Scroll()
        {
            wrapTest(() =>
            {
                CheckConditions[] allPressAndReleased(float amount) => new CheckConditions[]
                {
                    new ScrollCheckConditions(none, 1, 1, 1, amount),
                    new ScrollCheckConditions(noneExact, 1, 1, 1, amount),
                    new ScrollCheckConditions(noneModifiers, 1, 1, 1, amount),
                    new ScrollCheckConditions(unique, 1, 1, 1, amount),
                    new ScrollCheckConditions(all, 1, 1, 1, amount)
                };

                scrollMouseWheel(0, 2);
                check(TestAction.WheelUp, allPressAndReleased(2));
                scrollMouseWheel(0, -3);
                check(TestAction.WheelDown, allPressAndReleased(3));
                toggleKey(Key.ControlLeft);
                scrollMouseWheel(0, 4);
                toggleKey(Key.ControlLeft);
                check(TestAction.Ctrl_and_WheelUp, allPressAndReleased(4));
            });
        }

        private class EventCounts
        {
            public int OnPressedCount;
            public int OnReleasedCount;
            public int OnScrollCount;
        }

        private class CheckConditions
        {
            public readonly KeyBindingTester Tester;
            public readonly int OnPressedDelta;
            public readonly int OnReleasedDelta;

            public CheckConditions(KeyBindingTester tester, int onPressedDelta, int onReleasedDelta)
            {
                Tester = tester;
                OnPressedDelta = onPressedDelta;
                OnReleasedDelta = onReleasedDelta;
            }
        }

        private class ScrollCheckConditions : CheckConditions
        {
            public readonly int OnScrollCount;
            public readonly float LastScrollAmount;

            public ScrollCheckConditions(KeyBindingTester tester, int onPressedDelta, int onReleasedDelta, int onScrollCount, float lastScrollAmount)
                : base(tester, onPressedDelta, onReleasedDelta)
            {
                OnScrollCount = onScrollCount;
                LastScrollAmount = lastScrollAmount;
            }
        }

        private enum TestAction
        {
            A,
            S,
            D_or_F,
            Ctrl_A,
            Ctrl_S,
            Ctrl_D_or_F,
            Alt_A,
            Alt_S,
            Alt_D_or_F,
            LShift_A,
            LShift_S,
            LShift_D_or_F,
            RShift_A,
            RShift_S,
            RShift_D_or_F,
            Ctrl_Shift_A,
            Ctrl_Shift_S,
            Ctrl_Shift_D_or_F,
            Ctrl,
            RShift,
            LShift,
            Alt,
            Alt_and_LShift,
            Ctrl_and_Alt,
            Ctrl_or_Shift,
            LeftMouse,
            RightMouse,
            WheelUp,
            WheelDown,
            WheelLeft,
            WheelRight,
            Ctrl_and_WheelUp,
            AnyShift_A,
            AnyShift
        }

        private partial class TestInputManager : KeyBindingContainer<TestAction>
        {
            public TestInputManager(SimultaneousBindingMode concurrencyMode = SimultaneousBindingMode.None, KeyCombinationMatchingMode matchingMode = KeyCombinationMatchingMode.Any)
                : base(concurrencyMode, matchingMode)
            {
            }

            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.A, TestAction.A),
                new KeyBinding(InputKey.S, TestAction.S),
                new KeyBinding(InputKey.D, TestAction.D_or_F),
                new KeyBinding(InputKey.F, TestAction.D_or_F),

                new KeyBinding(new[] { InputKey.Control, InputKey.A }, TestAction.Ctrl_A),
                new KeyBinding(new[] { InputKey.Control, InputKey.S }, TestAction.Ctrl_S),
                new KeyBinding(new[] { InputKey.Control, InputKey.D }, TestAction.Ctrl_D_or_F),
                new KeyBinding(new[] { InputKey.Control, InputKey.F }, TestAction.Ctrl_D_or_F),

                new KeyBinding(new[] { InputKey.LShift, InputKey.A }, TestAction.LShift_A),
                new KeyBinding(new[] { InputKey.LShift, InputKey.S }, TestAction.LShift_S),
                new KeyBinding(new[] { InputKey.LShift, InputKey.D }, TestAction.LShift_D_or_F),
                new KeyBinding(new[] { InputKey.LShift, InputKey.F }, TestAction.LShift_D_or_F),

                new KeyBinding(new[] { InputKey.RShift, InputKey.A }, TestAction.RShift_A),
                new KeyBinding(new[] { InputKey.RShift, InputKey.S }, TestAction.RShift_S),
                new KeyBinding(new[] { InputKey.RShift, InputKey.D }, TestAction.RShift_D_or_F),
                new KeyBinding(new[] { InputKey.RShift, InputKey.F }, TestAction.RShift_D_or_F),

                new KeyBinding(new[] { InputKey.Shift, InputKey.A }, TestAction.AnyShift_A),

                new KeyBinding(new[] { InputKey.Alt, InputKey.A }, TestAction.Alt_A),
                new KeyBinding(new[] { InputKey.Alt, InputKey.S }, TestAction.Alt_S),
                new KeyBinding(new[] { InputKey.Alt, InputKey.D }, TestAction.Alt_D_or_F),
                new KeyBinding(new[] { InputKey.Alt, InputKey.F }, TestAction.Alt_D_or_F),

                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.A }, TestAction.Ctrl_Shift_A),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.S }, TestAction.Ctrl_Shift_S),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.D }, TestAction.Ctrl_Shift_D_or_F),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.F }, TestAction.Ctrl_Shift_D_or_F),

                new KeyBinding(new[] { InputKey.Control }, TestAction.Ctrl),
                new KeyBinding(new[] { InputKey.Shift }, TestAction.AnyShift),
                new KeyBinding(new[] { InputKey.LShift }, TestAction.LShift),
                new KeyBinding(new[] { InputKey.RShift }, TestAction.RShift),
                new KeyBinding(new[] { InputKey.Alt }, TestAction.Alt),
                new KeyBinding(new[] { InputKey.Alt, InputKey.LShift }, TestAction.Alt_and_LShift),
                new KeyBinding(new[] { InputKey.Control, InputKey.Alt }, TestAction.Ctrl_and_Alt),
                new KeyBinding(new[] { InputKey.Control }, TestAction.Ctrl_or_Shift),
                new KeyBinding(new[] { InputKey.Shift }, TestAction.Ctrl_or_Shift),

                new KeyBinding(new[] { InputKey.MouseLeft }, TestAction.LeftMouse),
                new KeyBinding(new[] { InputKey.MouseRight }, TestAction.RightMouse),

                new KeyBinding(new[] { InputKey.MouseWheelUp }, TestAction.WheelUp),
                new KeyBinding(new[] { InputKey.MouseWheelDown }, TestAction.WheelDown),
                new KeyBinding(new[] { InputKey.MouseWheelLeft }, TestAction.WheelLeft),
                new KeyBinding(new[] { InputKey.MouseWheelRight }, TestAction.WheelRight),
                new KeyBinding(new[] { InputKey.Control, InputKey.MouseWheelUp }, TestAction.Ctrl_and_WheelUp),
            };

            protected override bool Handle(UIEvent e)
            {
                base.Handle(e);
                return false;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;
        }

        private partial class ScrollTestButton : TestButton, IScrollBindingHandler<TestAction>
        {
            public int OnScrollCount { get; protected set; }
            public float LastScrollAmount { get; protected set; }

            public ScrollTestButton(TestAction action, SimultaneousBindingMode concurrency)
                : base(action, concurrency)
            {
                SpriteText.Font = SpriteText.Font.With(size: SpriteText.Font.Size * .9f);
            }

            protected override void Update()
            {
                base.Update();
                Text += $", {OnScrollCount}, {LastScrollAmount}";
            }

            public bool OnScroll(KeyBindingScrollEvent<TestAction> e)
            {
                if (Action == e.Action)
                {
                    ++OnScrollCount;
                    LastScrollAmount = e.ScrollAmount;
                }

                return false;
            }

            public override void Reset()
            {
                base.Reset();
                OnScrollCount = 0;
                LastScrollAmount = 0;
            }
        }

        private partial class TestButton : BasicButton, IKeyBindingHandler<TestAction>
        {
            public new readonly TestAction Action;
            public readonly SimultaneousBindingMode Concurrency;
            public int OnPressedCount { get; protected set; }
            public int OnReleasedCount { get; protected set; }

            private readonly Box highlight;
            private readonly string actionText;

            public TestButton(TestAction action, SimultaneousBindingMode concurrency)
            {
                Action = action;
                Concurrency = concurrency;

                BackgroundColour = Color4.SkyBlue;
                SpriteText.Font = SpriteText.Font.With(size: SpriteText.Font.Size * .8f);
                actionText = action.ToString().Replace('_', ' ');

                RelativeSizeAxes = Axes.X;
                Height = 30;
                Width = 0.3f;
                Padding = new MarginPadding(2);

                Background.Alpha = alphaTarget;

                Add(highlight = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                });
            }

            protected override void Update()
            {
                Text = $"{actionText}: {OnPressedCount}, {OnReleasedCount}";
                base.Update();
            }

            private float alphaTarget = 0.5f;

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                if (Action == e.Action)
                {
                    if (!e.Repeat)
                    {
                        if (Concurrency != SimultaneousBindingMode.All)
                            Trace.Assert(OnPressedCount == OnReleasedCount);
                        ++OnPressedCount;

                        alphaTarget += 0.2f;
                        Background.Alpha = alphaTarget;
                    }

                    highlight.ClearTransforms();
                    highlight.Alpha = 1f;
                    highlight.FadeOut(200);
                    return true;
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
                if (Action == e.Action)
                {
                    ++OnReleasedCount;

                    if (Concurrency != SimultaneousBindingMode.All)
                        Trace.Assert(OnPressedCount == OnReleasedCount);
                    else
                        Trace.Assert(OnReleasedCount <= OnPressedCount);

                    alphaTarget -= 0.2f;
                    Background.Alpha = alphaTarget;
                }
            }

            public virtual void Reset()
            {
                OnPressedCount = 0;
                OnReleasedCount = 0;
            }
        }

        private partial class KeyBindingTester : FillFlowContainer
        {
            private readonly TestButton[] testButtons;

            public KeyBindingTester(SimultaneousBindingMode concurrency, KeyCombinationMatchingMode matchingMode)
            {
                RelativeSizeAxes = Axes.Both;
                Direction = FillDirection.Vertical;

                testButtons = Enum.GetValues(typeof(TestAction)).Cast<TestAction>().Select(t =>
                {
                    if (t.ToString().Contains("Wheel"))
                        return new ScrollTestButton(t, concurrency);

                    return new TestButton(t, concurrency);
                }).ToArray();

                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = $"{concurrency} / {matchingMode}"
                    },
                    new TestInputManager(concurrency, matchingMode)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = testButtons
                        }
                    },
                };
            }

            public TestButton this[TestAction action] => testButtons.First(x => x.Action == action);
        }
    }
}
