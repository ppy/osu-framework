// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Testing;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    public class TestCaseKeyBindings : GridTestCase
    {
        public override string Description => @"Keybindings";

        public TestCaseKeyBindings()
            : base(2, 2)
        {

        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Cell(0).Add(new KeyBindingTester(SimultaneousBindingMode.None));
            Cell(1).Add(new KeyBindingTester(SimultaneousBindingMode.Unique));
            Cell(2).Add(new KeyBindingTester(SimultaneousBindingMode.All));
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
            Ctrl_Or_Shift
        }

        private class TestInputManager : KeyBindingInputManager<TestAction>
        {
            public TestInputManager(SimultaneousBindingMode concurrencyMode = SimultaneousBindingMode.None) : base(concurrencyMode)
            {
            }

            protected override IEnumerable<KeyBinding> CreateDefaultMappings() => new[]
            {
                new KeyBinding(Key.A, TestAction.A ),
                new KeyBinding(Key.S, TestAction.S ),
                new KeyBinding(Key.D, TestAction.D_or_F ),
                new KeyBinding(Key.F, TestAction.D_or_F ),

                new KeyBinding(new[] { Key.LControl, Key.A }, TestAction.Ctrl_A ),
                new KeyBinding(new[] { Key.LControl, Key.S }, TestAction.Ctrl_S ),
                new KeyBinding(new[] { Key.LControl, Key.D }, TestAction.Ctrl_D_or_F ),
                new KeyBinding(new[] { Key.LControl, Key.F }, TestAction.Ctrl_D_or_F ),

                new KeyBinding(new[] { Key.LShift, Key.A }, TestAction.Shift_A ),
                new KeyBinding(new[] { Key.LShift, Key.S }, TestAction.Shift_S ),
                new KeyBinding(new[] { Key.LShift, Key.D }, TestAction.Shift_D_or_F ),
                new KeyBinding(new[] { Key.LShift, Key.F }, TestAction.Shift_D_or_F ),

                new KeyBinding(new[] { Key.LControl, Key.LShift, Key.A }, TestAction.Ctrl_Shift_A ),
                new KeyBinding(new[] { Key.LControl, Key.LShift, Key.S }, TestAction.Ctrl_Shift_S),
                new KeyBinding(new[] { Key.LControl, Key.LShift, Key.D }, TestAction.Ctrl_Shift_D_or_F),
                new KeyBinding(new[] { Key.LControl, Key.LShift, Key.F }, TestAction.Ctrl_Shift_D_or_F),

                new KeyBinding(new[] { Key.LControl }, TestAction.Ctrl),
                new KeyBinding(new[] { Key.LShift }, TestAction.Shift),
                new KeyBinding(new[] { Key.LControl, Key.LShift }, TestAction.Ctrl_And_Shift),
                new KeyBinding(new[] { Key.LControl }, TestAction.Ctrl_Or_Shift),
                new KeyBinding(new[] { Key.LShift }, TestAction.Ctrl_Or_Shift),
            };
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
