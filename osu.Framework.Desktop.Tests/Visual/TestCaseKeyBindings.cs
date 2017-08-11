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

            Cell(0).Add(new KeyBindingTester(ConcurrentActionMode.None));
            Cell(1).Add(new KeyBindingTester(ConcurrentActionMode.UniqueActions));
            Cell(2).Add(new KeyBindingTester(ConcurrentActionMode.All));
        }

        private enum TestAction
        {
            A,
            S,
            D_or_F,
            Ctrl_A,
            Ctrl_S,
            Ctrl_D_or_F,
            Ctrl_Alt_A,
            Ctrl_Alt_S,
            Ctrl_Alt_D_or_F,
            Ctrl,
            Alt,
            Ctrl_And_Alt,
            //Ctrl_Or_Alt
        }

        private class TestInputManager : KeyBindingInputManager<TestAction>
        {
            public TestInputManager(ConcurrentActionMode concurrencyMode = ConcurrentActionMode.None) : base(concurrencyMode)
            {
            }

            protected override IDictionary<KeyCombination, TestAction> CreateDefaultMappings() => new Dictionary<KeyCombination, TestAction>
            {
                { Key.A, TestAction.A },
                { Key.S, TestAction.S },
                { Key.D, TestAction.D_or_F },
                { Key.F, TestAction.D_or_F },

                { new[] { Key.LControl, Key.A }, TestAction.Ctrl_A },
                { new[] { Key.LControl, Key.S }, TestAction.Ctrl_S },
                { new[] { Key.LControl, Key.D }, TestAction.Ctrl_D_or_F },
                { new[] { Key.LControl, Key.F }, TestAction.Ctrl_D_or_F },

                { new[] { Key.LControl, Key.LAlt, Key.A }, TestAction.Ctrl_Alt_A },
                { new[] { Key.LControl, Key.LAlt, Key.S }, TestAction.Ctrl_Alt_S },
                { new[] { Key.LControl, Key.LAlt, Key.D }, TestAction.Ctrl_Alt_D_or_F },
                { new[] { Key.LControl, Key.LAlt, Key.F }, TestAction.Ctrl_Alt_D_or_F },

                { new[] { Key.LControl }, TestAction.Ctrl },
                { new[] { Key.LAlt }, TestAction.Alt },
                { new[] { Key.LControl, Key.LAlt }, TestAction.Ctrl_And_Alt },
                //{ new[] { Key.LControl }, TestAction.Ctrl_Or_Alt },
                //{ new[] { Key.LAlt }, TestAction.Ctrl_Or_Alt },
            };
        }

        private class TestButton : Button, IHandleKeyBindings<TestAction>
        {
            private readonly TestAction action;

            public TestButton(TestAction action)
            {
                this.action = action;

                BackgroundColour = Color4.DarkBlue;
                Text = action.ToString().Replace('_', ' ');

                RelativeSizeAxes = Axes.X;
                Height = 40;
                Width = 0.3f;
                Padding = new MarginPadding(2);
            }

            public bool OnPressed(TestAction action)
            {
                if (this.action == action)
                {
                    BackgroundColour = Color4.Red;
                    Background.FlashColour(Color4.White, 100);
                    return true;
                }

                return false;
            }

            public bool OnReleased(TestAction action)
            {
                if (this.action == action)
                {
                    BackgroundColour = Color4.DarkBlue;
                    return true;
                }

                return false;
            }
        }

        private class KeyBindingTester : Container
        {
            public KeyBindingTester(ConcurrentActionMode concurrency)
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
