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
    internal class SampleSDL3Factory : SampleFactory, SDL3AudioDecoderManager.ISDL3AudioDataReceiver
    {
        private volatile bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private readonly SDL3AudioMixer mixer;
        private readonly SDL_AudioSpec spec;

        private float[] decodedAudio = Array.Empty<float>();

        private readonly AutoResetEvent completion = new AutoResetEvent(false);

        public SampleSDL3Factory(string name, SDL3AudioMixer mixer, int playbackConcurrency, SDL_AudioSpec spec, Stream data, SDL3AudioDecoderManager decoderManager)
            : base(name, playbackConcurrency)
        {
            this.mixer = mixer;
            this.spec = spec;

            decoderManager.StartDecodingAsync(data, spec, false, this);
        }

        void SDL3AudioDecoderManager.ISDL3AudioDataReceiver.GetData(byte[] audio, int byteLen, bool done)
        {
            if (IsDisposed)
                return;

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
                completion.WaitOne(); // may cause deadlock in bad situation, but needed to get tests passed

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

            decodedAudio = Array.Empty<float>();

            completion.Dispose();
            base.Dispose(disposing);
        }
    }
}
