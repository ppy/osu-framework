// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual.Clocks;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneTrackBass : FrameworkTestScene
    {
        private TrackBass track;

        [BackgroundDependencyLoader]
        private void load(Game game, AudioManager audio)
        {
            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);

            var stream = game.Resources.GetStream("Tracks/sample-track.mp3");
            track = new TrackBass(stream);
            audio.Track.AddItem(track);

            Child = new TestSceneClock.VisualClock(track)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativePositionAxes = Axes.Both,
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Seek to 1 second", () => track.Seek(1000));
            AddAssert("Initial seek was successful", () => track.CurrentTime == 1000);
        }

        /// <summary>
        /// Bass does not allow seeking to the end of the track. It should fail and the current time should not change.
        /// </summary>
        [Test]
        public void TestTrackSeekingToEndFails()
        {
            AddStep("Attempt to seek to the end of the track", () => track.Seek(track.Length));
            AddAssert("Track did not seek", () => track.CurrentTime == 1000);
        }

        /// <summary>
        /// Bass restarts the track from the beginning if Start is called when the track has been completed.
        /// This is blocked locally in <see cref="TrackBass"/>, so this test expects the track to not restart.
        /// </summary>
        [Test]
        public void TestTrackPlaybackBlocksAtTrackEnd()
        {
            AddStep("Seek to right before end of track", () => track.Seek(track.Length - 1));
            AddStep("Play", () => track.Start());
            AddUntilStep("Track stopped playing", () => !track.IsRunning && track.CurrentTime == track.Length);
            AddStep("Start track again", () => track.Start());
            AddAssert("Track did not restart", () => track.CurrentTime == track.Length);
        }
    }
}
