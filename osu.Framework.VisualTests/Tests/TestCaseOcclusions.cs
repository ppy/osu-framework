// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseOcclusions : TestCase
    {
        protected override Container<Drawable> Content => staticContainer;
        private Container staticContainer;

        public override void Reset()
        {
            base.Reset();

            AddInternal(staticContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300)
            });

            AddStep("Non-occluded", () => loadTest(0));
            AddStep("Barely non-occluded", () => loadTest(1));
            AddStep("Barely occluded", () => loadTest(2));
        }

        private void loadTest(int testCase)
        {
            staticContainer.Clear();
            staticContainer.Add(new OccludingBox { RelativeSizeAxes = Axes.Both });

            switch (testCase)
            {
                case 0:
                    Add(new[]
                    {
                        new TestBox
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.BottomCentre
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreLeft
                        },
                        new TestBox
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.TopCentre
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreRight
                        },
                    });
                    break;
                case 1:
                    Add(new[]
                    {
                        new TestBox
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight
                        },
                        new TestBox
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft
                        },
                    });
                    break;
                case 2:
                    Add(new[]
                    {
                        new TestBox
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Y = 1
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            X = -1
                        },
                        new TestBox
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Y = -1
                        },
                        new TestBox
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            X = 1
                        },
                    });
                    break;
            }
        }

        private class TestBox : Box
        {
            public TestBox()
            {
                Size = new Vector2(20);
                Colour = Color4.Green;
            }
        }

        private class OccludingBox : Box, IOccluder
        {
        }
    }
}
