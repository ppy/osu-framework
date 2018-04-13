// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseKeyBindings : GridTestCase
    {
        public TestCaseKeyBindings()
            : base(2, 2)
        {

        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Cell(0).Add(new KeyBindingTester(SimultaneousBindingMode.None));
            Cell(1).Add(new KeyBindingTester(SimultaneousBindingMode.NoneExact));
            Cell(2).Add(new KeyBindingTester(SimultaneousBindingMode.Unique));
            Cell(3).Add(new KeyBindingTester(SimultaneousBindingMode.All));
        }

        private enum TestAction
        {
            A,
            S,
            D_or_F,
            Ctrl_A,
            Ctrl_S,
            Ctrl_D_or_F,
            Shift_A,
            Shift_S,
            Shift_D_or_F,
            Ctrl_Shift_A,
            Ctrl_Shift_S,
            Ctrl_Shift_D_or_F,
            Ctrl,
            Shift,
            Ctrl_And_Shift,
            Ctrl_Or_Shift,
            LeftMouse,
            RightMouse
        }

        private class TestInputManager : KeyBindingContainer<TestAction>
        {
            public TestInputManager(SimultaneousBindingMode concurrencyMode = SimultaneousBindingMode.None) : base(concurrencyMode)
            {
            }

            public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.A, TestAction.A ),
                new KeyBinding(InputKey.S, TestAction.S ),
                new KeyBinding(InputKey.D, TestAction.D_or_F ),
                new KeyBinding(InputKey.F, TestAction.D_or_F ),

                new KeyBinding(new[] { InputKey.Control, InputKey.A }, TestAction.Ctrl_A ),
                new KeyBinding(new[] { InputKey.Control, InputKey.S }, TestAction.Ctrl_S ),
                new KeyBinding(new[] { InputKey.Control, InputKey.D }, TestAction.Ctrl_D_or_F ),
                new KeyBinding(new[] { InputKey.Control, InputKey.F }, TestAction.Ctrl_D_or_F ),

                new KeyBinding(new[] { InputKey.Shift, InputKey.A }, TestAction.Shift_A ),
                new KeyBinding(new[] { InputKey.Shift, InputKey.S }, TestAction.Shift_S ),
                new KeyBinding(new[] { InputKey.Shift, InputKey.D }, TestAction.Shift_D_or_F ),
                new KeyBinding(new[] { InputKey.Shift, InputKey.F }, TestAction.Shift_D_or_F ),

                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.A }, TestAction.Ctrl_Shift_A ),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.S }, TestAction.Ctrl_Shift_S),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.D }, TestAction.Ctrl_Shift_D_or_F),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift, InputKey.F }, TestAction.Ctrl_Shift_D_or_F),

                new KeyBinding(new[] { InputKey.Control }, TestAction.Ctrl),
                new KeyBinding(new[] { InputKey.Shift }, TestAction.Shift),
                new KeyBinding(new[] { InputKey.Control, InputKey.Shift }, TestAction.Ctrl_And_Shift),
                new KeyBinding(new[] { InputKey.Control }, TestAction.Ctrl_Or_Shift),
                new KeyBinding(new[] { InputKey.Shift }, TestAction.Ctrl_Or_Shift),

                new KeyBinding(new[] { InputKey.MouseLeft }, TestAction.LeftMouse),
                new KeyBinding(new[] { InputKey.MouseRight }, TestAction.RightMouse),
            };

            protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
            {
                base.OnKeyDown(state, args);
                return false;
            }

            protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
            {
                base.OnKeyUp(state, args);
                return false;
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                base.OnMouseDown(state, args);
                return false;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                base.OnMouseUp(state, args);
                return false;
            }

            protected override bool OnWheel(InputState state)
            {
                base.OnWheel(state);
                return false;
            }

            public override bool ReceiveMouseInputAt(Vector2 screenSpacePos) => true;
        }

        private class TestButton : Button, IKeyBindingHandler<TestAction>
        {
            private readonly TestAction action;

            public TestButton(TestAction action)
            {
                this.action = action;

                BackgroundColour = Color4.SkyBlue;
                Text = action.ToString().Replace('_', ' ');

                RelativeSizeAxes = Axes.X;
                Height = 40;
                Width = 0.3f;
                Padding = new MarginPadding(2);

                Background.Alpha = alphaTarget;
            }

            private float alphaTarget = 0.5f;

            public bool OnPressed(TestAction action)
            {
                if (this.action == action)
                {
                    alphaTarget += 0.2f;
                    Background.FadeTo(alphaTarget, 100, Easing.OutQuint);

                    return true;
                }

                return false;
            }

            public bool OnReleased(TestAction action)
            {
                if (this.action == action)
                {
                    alphaTarget -= 0.2f;
                    Background.FadeTo(alphaTarget, 100, Easing.OutQuint);

                    return true;
                }

                return false;
            }
        }

        private class KeyBindingTester : Container
        {
            public KeyBindingTester(SimultaneousBindingMode concurrency)
            {
                RelativeSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = concurrency.ToString(),
                    },
                    new TestInputManager(concurrency)
                    {
                        Y = 30,
                        RelativeSizeAxes = Axes.Both,
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ChildrenEnumerable = Enum.GetValues(typeof(TestAction)).Cast<TestAction>().Select(t => new TestButton(t))
                        }
                    },
                };
            }
        }
    }
}
