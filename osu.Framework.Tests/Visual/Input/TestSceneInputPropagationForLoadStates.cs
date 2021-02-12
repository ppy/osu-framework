// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    [HeadlessTest]
    public class TestSceneInputPropagationForLoadStates : FrameworkTestScene
    {
        private InputReceiver receiver1;
        private InputReceiver receiver2;

        [Test]
        public void TestReadyStateDrawablesDontReceiveInput()
        {
            AddStep("setup children", () =>
            {
                Children = new Drawable[]
                {
                    receiver1 = new InputReceiver(),
                    new InputSender(),
                    // receiver is updated (ie. LoadComplete is run) after sender, so it should not receive the initial input
                    receiver2 = new InputReceiver()
                };
            });

            AddAssert("receiver 1 received input'", () => receiver1.DidReceiveInput);
            AddAssert("receiver 2 did not receive input'", () => !receiver2.DidReceiveInput);
        }

        public class InputReceiver : Drawable
        {
            public bool DidReceiveInput;

            protected override bool Handle(UIEvent e)
            {
                DidReceiveInput = true;
                return base.Handle(e);
            }
        }

        public class InputSender : Drawable
        {
            protected override void LoadComplete()
            {
                base.LoadComplete();
                var inputManager = GetContainingInputManager();
                new KeyboardKeyInput(Key.A, true).Apply(inputManager.CurrentState, inputManager);
            }
        }
    }
}
