using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual
{
    class TestCaseCircularContainer : TestCase
    {
        public override string Description => "Checking for bugged corner radius (LOW FPS)";

        public TestCaseCircularContainer()
        {
            CircularContainer circularContainer;

            Add(circularContainer = new TestCircularContainer()
            {
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Blending = BlendingMode.Additive,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                BorderThickness = 5,
                BorderColour = Color4.White,
                Children = new[]
                {
                    new Box
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Width = 128f,
                        Height = 128f,
                        Colour = Color4.Green,
                    }
                },
            });
        }

        private class TestCircularContainer : CircularContainer
        {
            private int i = 0;
            private bool isExpanded = false;
            public bool IsExpanded
            {
                get { return isExpanded; }
                set
                {
                    isExpanded = value;

                    this.ScaleTo(isExpanded ? 2f : 1, 0);
                    Child.FadeTo(isExpanded ? 1f : 0, 0);
                }
            }

            protected override void Update()
            {
                base.Update();

                if (i++ % 10 == 0)
                    IsExpanded = !IsExpanded;
            }
        }
    }
}
