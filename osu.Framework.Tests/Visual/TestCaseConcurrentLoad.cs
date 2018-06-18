// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseConcurrentLoad : TestCase
    {
        private readonly FillFlowContainer flow;

        public TestCaseConcurrentLoad()
        {
            Child = flow = new FillFlowContainer
            {
                Direction = FillDirection.Full,
                RelativeSizeAxes = Axes.Both,
                Spacing = new Vector2(5)
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            for (int i = 0; i < 1000; i++)
                LoadComponentAsync(new TimeDrawable(), flow.Add);
        }

        public class TimeDrawable : CompositeDrawable
        {
            public TimeDrawable()
            {
                Size = new Vector2(15);
                InternalChild = new Box
                {
                    Colour = Color4.NavajoWhite,
                    RelativeSizeAxes = Axes.Both
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Thread.Sleep(20);
            }
        }
    }
}
