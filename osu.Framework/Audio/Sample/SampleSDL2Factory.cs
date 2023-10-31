// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using osu.Framework.Audio.Mixing.SDL2;
using osu.Framework.Bindables;
using SDL2;

namespace osu.Framework.Audio.Sample
{
    internal class SampleSDL2Factory : SampleFactory
    {
        private bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private readonly SDL2AudioMixer mixer;
        private readonly SDL.SDL_AudioSpec spec;
        private readonly AudioDecoder decoder;

        public float[]? DecodedAudio { get; private set; }

        private Stream? stream;

        public SampleSDL2Factory(Stream stream, string name, SDL2AudioMixer mixer, int playbackConcurrency, SDL.SDL_AudioSpec spec, AudioDecoder decoder)
            : base(name, playbackConcurrency)
        {
            this.stream = stream;
            this.mixer = mixer;
            this.spec = spec;
            this.decoder = decoder;
        }

        private protected override void LoadSample()
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(!IsLoaded);

            if (stream == null)
                return;

            try
            {
                byte[] audio = decoder.DecodeAudioInCurrentSpec(stream);

                if (audio.Length > 0)
                {
                    DecodedAudio = new float[audio.Length / 4];
                    Buffer.BlockCopy(audio, 0, DecodedAudio, 0, audio.Length);
                }

                Length = audio.Length / 4.0d / spec.freq / spec.channels;
                isLoaded = true;
            }
            finally
            {
                stream.Dispose();
                stream = null;
            }
        }

        public SampleSDL2AudioPlayer CreatePlayer() => new SampleSDL2AudioPlayer(DecodedAudio ?? Array.Empty<float>(), spec.freq, spec.channels);

        public override Sample CreateSample() => new SampleSDL2(this, mixer) { OnPlay = SampleFactoryOnPlay };

        private protected override void FreeSample()
        {
            // All players created by this factory have reference to this array.
            // It removes its own reference to the array, but GC will clear it once all SampleAudioPlayers for this sample are gone.
            DecodedAudio = null;
        }

        private protected override void UpdatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
        }

        ~SampleSDL2Factory()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            stream?.Dispose();
            stream = null;

            base.Dispose(disposing);
        }
    }
}
