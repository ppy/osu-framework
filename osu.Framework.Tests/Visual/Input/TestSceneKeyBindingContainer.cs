// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneKeyBindingContainer : FrameworkTestScene
    {
        [Test]
        public void TestTriggerWithNoKeyBindings()
        {
            bool pressedReceived = false;
            bool releasedReceived = false;

            TestKeyBindingContainer keyBindingContainer = null;

            AddStep("add container", () =>
            {
                pressedReceived = false;
                releasedReceived = false;

                Clear();

                Add(keyBindingContainer = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = _ => pressedReceived = true,
                        Released = _ => releasedReceived = true
                    }
                });
            });

            AddStep("trigger press", () => keyBindingContainer.TriggerPressed(TestAction.Action1));
            AddAssert("press received", () => pressedReceived);

            AddStep("trigger release", () => keyBindingContainer.TriggerReleased(TestAction.Action1));
            AddAssert("release received", () => releasedReceived);
        }

        private class TestKeyBindingReceptor : Drawable, IKeyBindingHandler<TestAction>
        {
            public Action<TestAction> Pressed;
            public Action<TestAction> Released;

            public TestKeyBindingReceptor()
            {
                RelativeSizeAxes = Axes.Both;
            }

            public bool OnPressed(TestAction action)
            {
                Pressed?.Invoke(action);
                return true;
            }

            public bool OnReleased(TestAction action)
            {
                Released?.Invoke(action);
                return true;
            }
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<KeyBinding> DefaultKeyBindings => null;
        }

        private enum TestAction
        {
            Action1
        }
    }
}
