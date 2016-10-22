// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transformations;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseScrollableFlow : TestCase
    {
        private ScheduledDelegate boxCreator;

        public override string Name => @"Scrollable Flow";
        public override string Description => @"A flow container in a scroll container";

        public override void Reset()
        {
            base.Reset();

            FlowContainer flow;

            Children = new []
            {
                new ScrollContainer
                {
                    Children = new []
                    {
                        flow = new FlowContainer
                        {
                            LayoutDuration = 100,
                            LayoutEasing = EasingTypes.Out,
                            Spacing = new Vector2(1, 1),
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding(5)
                        }
                    },
                },
            };

            boxCreator?.Cancel();
            boxCreator = Scheduler.AddDelayed(delegate
            {
                if (Parent == null) return;

                Box box;
                Container container = new Container
                {
                    Size = new Vector2(80, 80),
                    Children = new[] {
                        box = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = new Color4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1)
                        }
                    }
                };

                flow.Add(container);

                container.FadeInFromZero(1000);
                container.Delay(RNG.Next(0, 20000), true);
                container.FadeOutFromOne(4000);
                box.RotateTo((RNG.NextSingle() - 0.5f) * 90, 4000);
                box.ScaleTo(0.5f, 4000);
                container.Expire();
            }, 100, true);

            Scheduler.Add(boxCreator);
        }
    }
}
