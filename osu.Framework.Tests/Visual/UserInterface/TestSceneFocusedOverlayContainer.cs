// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneFocusedOverlayContainer : ManualInputManagerTestScene
    {
        private TestFocusedOverlayContainer overlayContainer;

        private ParentContainer parentContainer;

        [Test]
        public void TestClickDismiss()
        {
            AddStep("create container", () => { Child = overlayContainer = new TestFocusedOverlayContainer(); });

            AddStep("show", () => overlayContainer.Show());
            AddAssert("has focus", () => overlayContainer.HasFocus);

            AddStep("click inside", () =>
            {
                InputManager.MoveMouseTo(overlayContainer.ScreenSpaceDrawQuad.Centre);
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("still has focus", () => overlayContainer.HasFocus);

            AddStep("click outside", () =>
            {
                InputManager.MoveMouseTo(overlayContainer.ScreenSpaceDrawQuad.TopLeft - new Vector2(20));
                InputManager.PressButton(MouseButton.Left);
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("lost focus", () => !overlayContainer.HasFocus);
            AddAssert("not visible", () => overlayContainer.State.Value == Visibility.Hidden);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestScrollBlocking(bool isBlocking)
        {
            AddStep("create container", () =>
            {
                Child = parentContainer = new ParentContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        overlayContainer = new TestFocusedOverlayContainer(blockScrollInput: isBlocking)
                    }
                };
            });

            AddStep("show", () => overlayContainer.Show());

            AddAssert("has focus", () => overlayContainer.HasFocus);

            int initialScrollCount = 0;

            AddStep("scroll inside", () =>
            {
                initialScrollCount = parentContainer.ScrollReceived;
                InputManager.MoveMouseTo(overlayContainer.ScreenSpaceDrawQuad.Centre);
                InputManager.ScrollVerticalBy(1);
            });

            if (isBlocking)
                AddAssert("scroll not received by parent", () => parentContainer.ScrollReceived == initialScrollCount);
            else
                AddAssert("scroll received by parent", () => parentContainer.ScrollReceived == ++initialScrollCount);

            AddStep("scroll outside", () =>
            {
                InputManager.MoveMouseTo(overlayContainer.ScreenSpaceDrawQuad.TopLeft - new Vector2(20));
                InputManager.ScrollVerticalBy(1);
            });

            AddAssert("scroll received by parent", () => parentContainer.ScrollReceived == ++initialScrollCount);
        }

        private class TestFocusedOverlayContainer : FocusedOverlayContainer
        {
            protected override bool StartHidden { get; }

            protected override bool BlockPositionalInput => true;

            protected override bool BlockNonPositionalInput => false;

            protected override bool BlockScrollInput { get; }

            public TestFocusedOverlayContainer(bool startHidden = true, bool blockScrollInput = true)
            {
                BlockScrollInput = blockScrollInput;

                StartHidden = startHidden;

                Size = new Vector2(0.5f);
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Cyan,
                        RelativeSizeAxes = Axes.Both,
                    },
                };

                State.ValueChanged += e => FireCount++;
            }

            public int FireCount { get; private set; }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnClick(ClickEvent e)
            {
                if (!base.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                {
                    Hide();
                    return true;
                }

                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                base.OnMouseDown(e);
                return true;
            }

            protected override void PopIn()
            {
                this.FadeIn(1000, Easing.OutQuint);
                this.ScaleTo(1, 1000, Easing.OutElastic);

                base.PopIn();
            }

            protected override void PopOut()
            {
                this.FadeOut(1000, Easing.OutQuint);
                this.ScaleTo(0.4f, 1000, Easing.OutQuint);

                base.PopOut();
            }
        }
    }

    public class ParentContainer : Container
    {
        public int ScrollReceived;

        protected override bool OnScroll(ScrollEvent e)
        {
            ScrollReceived++;
            return base.OnScroll(e);
        }
    }
}
