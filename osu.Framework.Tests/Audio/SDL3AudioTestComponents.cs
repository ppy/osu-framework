// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Tests.Audio
{
    /// <summary>
    /// Provides a SDL3 audio pipeline to be used for testing audio components.
    /// </summary>
    public class SDL3AudioTestComponents : AudioTestComponents, IDisposable
    {
        private SDL3BaseAudioManager baseManager = null!;

        public SDL3AudioTestComponents(bool init = true)
            : base(init)
        {
        }

        protected override void Prepare()
        {
            base.Prepare();
            baseManager = new SDL3BaseAudioManager(MixerComponents.Items.OfType<SDL3AudioMixer>);
        }

        public override void Init()
        {
            SDL_SetHint(SDL_HINT_AUDIO_DRIVER, "dummy"u8);

            if (SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO) < 0)
                throw new InvalidOperationException($"Failed to initialise SDL: {SDL_GetError()}");

            if (!baseManager.SetAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK))
                throw new InvalidOperationException($"Failed to open SDL3 audio device: {SDL_GetError()}");
        }

        public override AudioMixer CreateMixer()
        {
            var mixer = new SDL3AudioMixer(Mixer, "Test mixer");
            baseManager.RunWhileLockingAudioStream(() => MixerComponents.AddItem(mixer));

            return mixer;
        }

        public override void DisposeInternal()
        {
            base.DisposeInternal();
            baseManager.Dispose();

            SDL_Quit();
        }

        internal override Track CreateTrack(Stream data, string name) => new TrackSDL3(name, data, baseManager.AudioSpec, 441);

        internal override SampleFactory CreateSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency)
            => new SampleSDL3Factory(stream, name, (SDL3AudioMixer)mixer, playbackConcurrency, baseManager.AudioSpec);
    }
}
