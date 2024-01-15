// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public partial class TouchInputTest : ManualInputManagerTestScene
    {
        /// <summary>
        /// Tests that a drawable whose parent is removed from the hierarchy (or is otherwise removed from the input queues) does not receive an OnDragStart() event.
        /// </summary>
        [Test]
        public void TestNoLongerValidChildDrawableDoesNotReceiveTouchMove()
        {
            InputReceptor receptor = null;
            Container receptorParent = null;
            Vector2 lastPosition = Vector2.Zero;

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptorParent = new Container
                    {
                        Children = new Drawable[]
                        {
                            receptor = new InputReceptor { Size = new Vector2(100) }
                        }
                    }
                };
            });

            AddStep("begin touch on receptor", () =>
            {
                lastPosition = receptor.ToScreenSpace(receptor.LayoutRectangle.Centre);
                InputManager.BeginTouch(new Touch(TouchSource.Touch1, lastPosition));
            });

            AddStep("remove receptor parent", () => Remove(receptorParent, true));

            AddStep("move touch", () => InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, lastPosition + new Vector2(10))));
            AddAssert("receptor did not receive touch move", () => !receptor.TouchMoveReceived);
        }

        private partial class InputReceptor : Box
        {
            public bool TouchMoveReceived { get; set; }

            protected override void OnTouchMove(TouchMoveEvent e)
            {
                TouchMoveReceived = true;
            }
        }
    }
}
