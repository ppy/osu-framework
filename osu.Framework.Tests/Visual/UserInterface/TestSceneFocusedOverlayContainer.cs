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

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneFocusedOverlayContainer : ManualInputManagerTestScene
    {
        private TestFocusedOverlayContainer testContainer;

        [Test]
        public void TestInputBlocking()
        {
            AddStep("create container", () => Child = testContainer = new TestFocusedOverlayContainer());

            AddStep("show", () => testContainer.Show());

            AddAssert("has focus", () => testContainer.HasFocus);
        }

        private class TestFocusedOverlayContainer : FocusedOverlayContainer
        {
            private readonly bool startHidden;

            protected override bool StartHidden => startHidden;

            protected override bool BlockPositionalInput => true;

            protected override bool BlockNonPositionalInput => false;

            public TestFocusedOverlayContainer(bool startHidden = true)
            {
                this.startHidden = startHidden;

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
}
