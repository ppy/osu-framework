// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using mysoundlib_cs;
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
    public unsafe class SDL3AudioTestComponents : AudioTestComponents
    {
        private IntPtr manager;

        public SDL3AudioTestComponents(bool init = true)
            : base(init)
        {
        }

        protected override void Prepare()
        {
            base.Prepare();

            SDL3AudioManager.PrepareLibrary();
            SDL_SetHint(SDL_HINT_AUDIO_DRIVER, "dummy"u8);
            manager = MySoundLibrary.mslCreateAudioManager();
        }

        public override void Init()
        {
            if (!MySoundLibrary.mslOpenAudioDevice(manager, null).ToBool())
                throw new InvalidOperationException($"Failed to open SDL3 audio device: {SDL_GetError()}");
        }

        public override AudioMixer CreateMixer()
        {
            var mixer = new SDL3AudioMixer(Mixer, "Test mixer");
            MySoundLibrary.mslAddMixer(manager, mixer.Handle);
            MixerComponents.AddItem(mixer);

            return mixer;
        }

        public void WaitUntilTrackIsLoaded(TrackSDL3 track)
        {
            // TrackSDL3 doesn't have data readily available right away after constructed.
            while (!MySoundLibrary.mslTrackIsLoaded(track.Handle).ToBool())
            {
                Update();
                Thread.Sleep(10);
            }
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();

            MySoundLibrary.mslDestroyAudioManager(manager);
            manager = IntPtr.Zero;
        }

        internal override Track CreateTrack(Stream data, string name) => new TrackSDL3(data, name);

        internal override SampleFactory CreateSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency)
            => new SampleSDL3Factory(stream, name, (SDL3AudioMixer)mixer, playbackConcurrency);
    }
}
