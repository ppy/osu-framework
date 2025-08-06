// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using static SDL.SDL3;

namespace osu.Framework.Tests.Audio
{
    /// <summary>
    /// Provides a SDL3 audio pipeline to be used for testing audio components.
    /// </summary>
    public class SDL3AudioTestComponents : AudioTestComponents
    {
        private SDL3AudioManager.SDL3BaseAudioManager baseManager = null!;

        public SDL3AudioTestComponents(bool init = true)
            : base(init)
        {
        }

        protected override void Prepare()
        {
            base.Prepare();

            SDL_SetHint(SDL_HINT_AUDIO_DRIVER, "dummy"u8);
            baseManager = new SDL3AudioManager.SDL3BaseAudioManager(MixerComponents.Items.OfType<SDL3AudioMixer>);
        }

        public override void Init()
        {
            if (!baseManager.SetAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK))
                throw new InvalidOperationException($"Failed to open SDL3 audio device: {SDL_GetError()}");
        }

        public override AudioMixer CreateMixer()
        {
            var mixer = new SDL3AudioMixer(Mixer, "Test mixer");
            baseManager.RunWhileLockingAudioStream(() => MixerComponents.AddItem(mixer));

            return mixer;
        }

        public void WaitUntilTrackIsLoaded(TrackSDL3 track)
        {
            // TrackSDL3 doesn't have data readily available right away after constructed.
            while (!track.IsCompletelyLoaded)
            {
                Update();
                Thread.Sleep(10);
            }
        }

        public override void DisposeInternal()
        {
            base.DisposeInternal();
            baseManager.Dispose();
        }

        internal override Track CreateTrack(Stream data, string name) => baseManager.GetNewTrack(data, name);

        internal override SampleFactory CreateSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency)
            => baseManager.GetSampleFactory(stream, name, mixer, playbackConcurrency);
    }
}
