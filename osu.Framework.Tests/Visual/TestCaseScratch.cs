// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseScratch : DrawFrameTestCase
    {
        private FillFlowContainer flow;
        private Box box;

        public TestCaseScratch()
        {
            Child = new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Width = 0.75f,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkSlateGray,
                        Name = "A"
                    },
                    flow = new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Width = 0.5f,
                        Name = "B",
                        Child = box = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 200
                        }
                    }
                }
            };

            AddStep("hide", () => SchedulerAfterChildren.Add(() => box.Hide()));
            AddStep("show", () => SchedulerAfterChildren.Add(() => box.Show()));
        }
    }
}
