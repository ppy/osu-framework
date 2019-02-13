// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Tests.Visual.TestCaseContainer;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFrostEffect : TestCaseMasking
    {
        public TestCaseFrostEffect()
        {
            Remove(TestContainer);

            BufferedContainer buffered;

            Add(buffered = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = TestContainer
            });

            Add(new BlurView(buffered));
        }

        private class BlurView : CompositeDrawable
        {
            public BlurView(BufferedContainer buffer)
            {
                Size = new Vector2(200);
                Masking = true;
                CornerRadius = 20;
                BorderColour = Color4.Magenta;
                BorderThickness = 2;

                InternalChild = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    BlurSigma = new Vector2(30),
                    Child = new BufferSprite(buffer)
                    {
                        RelativeSizeAxes = Axes.Both,
                        SynchronizedDrawQuad = true
                    }
                };

                AddInternal(infoText = new SpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = "You can drag this window.",
                    TextSize = 16
                });
            }

            private readonly SpriteText infoText;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                infoText
                    .Delay(5000)
                    .MoveToY(-infoText.TextSize, 500)
                    .Expire();
            }

            protected override bool OnDrag(DragEvent e)
            {
                Position += e.Delta;
                return true;
            }

            protected override bool OnDragEnd(DragEndEvent e) => true;
            protected override bool OnDragStart(DragStartEvent e) => true;
        }
    }
}