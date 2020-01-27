// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class MouseInputTest : ManualInputManagerTestScene
    {
        /// <summary>
        /// Tests that if a drawable is removed from the hierarchy (or is otherwise removed from the input queues),
        /// it won't receive an OnClick() event on mouse up.
        /// </summary>
        [Test]
        public void TestNoLongerValidDrawableDoesNotReceiveOnClick()
        {
            var receptors = new InputReceptor[3];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor { Size = new Vector2(100) },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("move mouse to receptors", () => InputManager.MoveMouseTo(receptors[0]));
            AddStep("press button", () => InputManager.PressButton(MouseButton.Left));

            AddStep("remove receptor 0", () => Remove(receptors[0]));

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("receptor 0 did not receive click", () => !receptors[0].ClickReceived);
        }

        /// <summary>
        /// Tests that a drawable that is removed and re-added to the hierarchy can still handle OnClick().
        /// </summary>
        [Test]
        public void TestReValidatedDrawableReceivesOnClick()
        {
            var receptors = new InputReceptor[3];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor { Size = new Vector2(100) },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("move mouse to receptors", () => InputManager.MoveMouseTo(receptors[0]));
            AddStep("press button", () => InputManager.PressButton(MouseButton.Left));

            AddStep("remove receptor 0", () => Remove(receptors[0]));
            AddStep("add back receptor 0", () => Add(receptors[0]));

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("receptor 0 received click", () => receptors[0].ClickReceived);
        }

        private class InputReceptor : Box
        {
            public bool ClickReceived { get; set; }

            protected override bool OnClick(ClickEvent e)
            {
                ClickReceived = true;
                return false;
            }
        }
    }
}
