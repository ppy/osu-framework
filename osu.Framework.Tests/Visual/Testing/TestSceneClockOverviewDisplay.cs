// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneClockOverviewDisplay : FrameworkTestScene
    {
        private ClockOverviewDisplay display;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new[]
            {
                display = new ClockOverviewDisplay(),
                new Container()
                {
                    Name = "Test Container 1",
                    Clock = new FramedClock(),
                    Children = new Drawable[]
                    {
                        new Container()
                        {
                            Name = "Nested Container 1",
                            Clock = new FramedOffsetClock(Clock) { Offset = 200 },
                        },
                        new Container()
                        {
                            Name = "Nested Container 2",
                            Clock = new FramedOffsetClock(Clock) { Offset = 800 },
                        },
                    }
                }
            };

            display.ToggleVisibility();
        }
    }
}
