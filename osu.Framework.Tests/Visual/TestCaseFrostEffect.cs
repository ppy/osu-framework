using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFrostEffect : TestCaseMasking
    {
        private readonly BufferedContainer buffer;

        public TestCaseFrostEffect()
        {
            Remove(TestContainer);

            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });

            Add(new BlurView(buffer));
        }

        class BlurView : CompositeDrawable
        {
            public BlurView(BufferedContainer buffer)
            {
                Size = new Vector2(200);
                Masking = true;
                CornerRadius = 20;
                BorderColour = Color4.Red;
                BorderThickness = 2;

                InternalChild = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    BlurSigma = new Vector2(30),
                    Child = new BufferedContainer.BufferSprite(buffer)
                    {
                        RelativeSizeAxes = Axes.Both,
                        SynchronizedDrawQuad = true
                    }
                };
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