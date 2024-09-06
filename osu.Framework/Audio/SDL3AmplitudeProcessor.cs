// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NAudio.Dsp;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;

namespace osu.Framework.Audio
{
    internal class SDL3AmplitudeProcessor
    {
        /// <summary>
        /// The most recent amplitude data. Note that this is updated on an ongoing basis and there is no guarantee it is in a consistent (single sample) state.
        /// If you need consistent data, make a copy of FrequencyAmplitudes while on the audio thread.
        /// </summary>
        public ChannelAmplitudes CurrentAmplitudes { get; private set; } = ChannelAmplitudes.Empty;

        private Complex[] fftSamples = new Complex[ChannelAmplitudes.AMPLITUDES_SIZE * 2];
        private float[] fftResult = new float[ChannelAmplitudes.AMPLITUDES_SIZE];

        public void Update(float[] samples, int channels)
        {
            if (samples.Length / channels < ChannelAmplitudes.AMPLITUDES_SIZE)
                return; // not enough data

            float leftAmplitude = 0;
            float rightAmplitude = 0;
            int secondCh = channels < 2 ? 0 : 1;
            int fftIndex = 0;

            for (int i = 0; i < samples.Length; i += channels)
            {
                leftAmplitude = Math.Max(leftAmplitude, Math.Abs(samples[i]));
                rightAmplitude = Math.Max(rightAmplitude, Math.Abs(samples[i + secondCh]));

                if (fftIndex < fftSamples.Length)
                {
                    fftSamples[fftIndex].Y = 0;
                    fftSamples[fftIndex++].X = samples[i] + samples[i + secondCh];
                }
            }

            FastFourierTransform.FFT(true, (int)Math.Log2(fftSamples.Length), fftSamples);

            for (int i = 0; i < fftResult.Length; i++)
                fftResult[i] = fftSamples[i].ComputeMagnitude();

            CurrentAmplitudes = new ChannelAmplitudes(Math.Min(1f, leftAmplitude), Math.Min(1f, rightAmplitude), fftResult);
        }
    }
}
