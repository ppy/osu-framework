// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Bindables;
using SDL;

namespace osu.Framework.Audio.Sample
{
    internal class SampleSDL3Factory : SampleFactory
    {
        private volatile bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private readonly SDL3AudioMixer mixer;
        private readonly SDL_AudioSpec spec;

        private float[] decodedAudio = Array.Empty<float>();

        private readonly AutoResetEvent completion = new AutoResetEvent(false);

        private SDL3AudioDecoderManager.AudioDecoder? decoder;

        public SampleSDL3Factory(Stream stream, string name, SDL3AudioMixer mixer, int playbackConcurrency, SDL_AudioSpec spec)
            : base(name, playbackConcurrency)
        {
            this.mixer = mixer;
            this.spec = spec;

            decoder = SDL3AudioManager.DecoderManager.StartDecodingAsync(spec.freq, spec.channels, spec.format, stream, ReceiveAudioData, false);
        }

        internal void ReceiveAudioData(byte[] audio, int byteLen, SDL3AudioDecoderManager.AudioDecoder data, bool done)
        {
            if (IsDisposed)
                return;

            decoder = null;

            if (byteLen > 0)
            {
                decodedAudio = new float[byteLen / 4];
                Buffer.BlockCopy(audio, 0, decodedAudio, 0, byteLen);
            }

            Length = byteLen / 4d / spec.freq / spec.channels * 1000d;
            isLoaded = true;

            completion.Set();
        }

        public SampleSDL3AudioPlayer CreatePlayer()
        {
            if (!isLoaded)
                completion.WaitOne(10);

            return new SampleSDL3AudioPlayer(decodedAudio, spec.freq, spec.channels);
        }

        public override Sample CreateSample() => new SampleSDL3(this, mixer) { OnPlay = SampleFactoryOnPlay };

        protected override void UpdatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
        }

        ~SampleSDL3Factory()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            decoder?.Stop();

            decodedAudio = Array.Empty<float>();

            completion.Dispose();
            base.Dispose(disposing);
        }
    }
}
