using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneHoverUpdates : ManualInputManagerTestScene
    {
        private TestDrawable hoveredDrawable;

        [SetUp]
        public override void SetUp() => Schedule(() =>
        {
            base.SetUp();

            Child = hoveredDrawable = new TestDrawable();

            InputManager.MoveMouseTo(Vector2.Zero);
        });

        [Test]
        public void TestCheckHoverFromMouseMove()
        {
            AddStep("move mouse inside drawable", () => InputManager.MoveMouseTo(hoveredDrawable));
            AddAssert("is hover correct", () => !hoveredDrawable.IncorrectHover);
        }

        private class TestDrawable : Box
        {
            public bool IncorrectHover = false;

            public TestDrawable()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Size = new Vector2(128);
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                if (!IncorrectHover && IsHovered != ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                    IncorrectHover = true;

                return base.OnMouseMove(e);
            }
        }
    }
}
