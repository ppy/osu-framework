// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Tests.Visual.Clocks
{
    public abstract class TestSceneClock : FrameworkTestScene
    {
        private readonly FillFlowContainer fill;

        protected TestSceneClock()
        {
            Child = fill = new FillFlowContainer
            {
                Spacing = new Vector2(5),
                Direction = FillDirection.Full,
                RelativeSizeAxes = Axes.Both,
            };

            AddStep("clear all", () =>
            {
                fill.Clear();
                lastClock = null;
                AddClock(Clock);
            });
        }

        private IClock lastClock;

        protected IClock AddClock(IClock clock)
        {
            if (lastClock != null && clock is ISourceChangeableClock framed)
                framed.ChangeSource(lastClock);

            fill.Add(new VisualClock(lastClock = clock) { ProcessFrame = true });

            return clock;
        }
    }
}
