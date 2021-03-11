// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Visualisation;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneAudioMixerOverlay : FrameworkTestScene
    {
        private AudioManager audio;

        private TrackBass bassTrack;
        private ITrackStore tracks;
        private DrawableSample sample;
        private AudioMixerOverlay mixerOverlay;

        protected override void Dispose(bool isDisposing)
        {
            bassTrack?.Dispose();
            sample?.Dispose();

            base.Dispose(isDisposing);
        }

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks, AudioManager audio)
        {
            this.tracks = tracks;
            this.audio = audio;

            Child = mixerOverlay = new AudioMixerOverlay(audio.Mixer);
            mixerOverlay.Show();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("idle", () =>
            {
                // do nothing
            });
            AddStep("load", () =>
            {
                bassTrack?.Dispose();
                bassTrack = (TrackBass)tracks.Get("sample-track.mp3");
            });
            AddStep("play", () =>
            {
                bassTrack?.Start();
            });
            AddStep("stop", () =>
            {
                bassTrack?.Stop();
            });
            AddStep("Play SFX1", () =>
            {
                sample = new DrawableSample(audio.Samples.Get("long.mp3"));
                sample?.Play();
            });
        }
    }
}
