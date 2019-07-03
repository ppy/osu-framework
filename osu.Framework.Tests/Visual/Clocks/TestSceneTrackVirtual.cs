// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public class TestSceneTrackVirtual : TestSceneClock
    {
        private TrackVirtual trackVirtual;
        private DecoupleableInterpolatingFramedClock decoupleableClock;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddStep("Decouple clock", () =>
            {
                AddClock(trackVirtual = new TrackVirtual(60000));
                AddClock(decoupleableClock = new DecoupleableInterpolatingFramedClock { IsCoupled = false });
                decoupleableClock.ChangeSource(trackVirtual);
                trackVirtual.ResetSpeedAdjustments();
            });
            AddSliderStep("Seek clock", 0, 60000, 0, i => trackVirtual?.Seek(i));
            AddSliderStep("Seek decoupled", 0, 60000, 0, i => decoupleableClock?.Seek(i));
            AddStep("Seek current time", () => decoupleableClock?.Seek(decoupleableClock.CurrentTime));
        }
    }
}
