//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Transformations;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseScrollableFlow : TestCase
    {
        private ScheduledDelegate boxCreator;

        internal override string Name => @"Scrollable Flow";
        internal override string Description => @"A flow container in a scroll container";

        internal override void Reset()
        {
            base.Reset();

            FlowContainer flow = new FlowContainer()
            {
                LayoutDuration = 100,
                LayoutEasing = EasingTypes.Out,
                Padding = new Vector2(1, 1)
            };

            boxCreator?.Cancel();

            boxCreator = Game.Scheduler.AddDelayed(delegate
            {
                if (Parent == null) return;

                Box box = new Box(new Color4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1)) { Size = new Vector2(80, 80) };
                box.FadeInFromZero(1000);
                box.Delay(RNG.Next(0, 20000));
                box.FadeOutFromOne(4000);
                box.Expire();

                flow.Add(box);
            }, 100, true);

            Game.Scheduler.Add(boxCreator);

            ScrollContainer scrolling = new ScrollContainer();
            scrolling.Add(flow);
            Add(scrolling);
        }

        private void awardMedal()
        {
            Medal medal = new Medal(@"all-secret-bunny", @"Don't let the bunny distract you!", @"The order was indeed, not a rabbit.");
            MedalPopup popup = new MedalPopup(medal);
            Game.ShowDialog(popup);
        }
    }
}
