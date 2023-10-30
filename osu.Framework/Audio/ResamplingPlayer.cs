// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NAudio.Dsp;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Abstract class that's meant to be used with a real player implementation.
    /// This class provides resampling on the fly for players.
    /// </summary>
    internal abstract class ResamplingPlayer
    {
        private double relativeRate = 1;

        /// <summary>
        /// Represents current relative rate.
        /// </summary>
        public double RelativeRate
        {
            get => relativeRate;
            set => setRate(value);
        }

        private WdlResampler? resampler;

        protected readonly int SrcRate;
        protected readonly byte SrcChannels;

        /// <summary>
        /// Creates a new <see cref="ResamplingPlayer"/>.
        /// </summary>
        /// <param name="srcRate">Sampling rate of audio that's given from <see cref="GetRemainingRawFloats(float[], int, int)"/> or <see cref="GetRemainingRawBytes(byte[])"/></param>
        /// <param name="srcChannels">Channels of audio that's given from <see cref="GetRemainingRawFloats(float[], int, int)"/> or <see cref="GetRemainingRawBytes(byte[])"/></param>
        protected ResamplingPlayer(int srcRate, byte srcChannels)
        {
            SrcRate = srcRate;
            SrcChannels = srcChannels;
        }

        /// <summary>
        /// Sets relative rate of audio.
        /// </summary>
        /// <param name="relativeRate">Rate that is relative to the original frequency. 1.0 is normal rate.</param>
        private void setRate(double relativeRate)
        {
            if (relativeRate == 0)
            {
                this.relativeRate = relativeRate;
                return;
            }

            if (relativeRate < 0 || this.relativeRate == relativeRate)
                return;

            if (resampler == null)
            {
                resampler = new WdlResampler();
                resampler.SetMode(true, 2, false);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false);
            }

            resampler.SetRates(SrcRate, SrcRate / relativeRate);
            this.relativeRate = relativeRate;
        }

        protected virtual double GetProcessingLatency()
        {
            if (resampler == null || RelativeRate == 1)
                return 0;

            return resampler.GetCurrentLatency() * 1000.0d;
        }

        /// <summary>
        /// Returns rate adjusted audio samples. It calls a parent method if <see cref="RelativeRate"/> is 1.
        /// </summary>
        /// <param name="data">An array to put samples in</param>
        /// <returns>The number of samples put into the array</returns>
        public virtual int GetRemainingSamples(float[] data)
        {
            if (RelativeRate == 0)
                return 0;

            if (resampler == null || RelativeRate == 1)
                return GetRemainingRawFloats(data, 0, data.Length);

            int requested = data.Length / SrcChannels;
            int needed = resampler.ResamplePrepare(requested, SrcChannels, out float[] inBuffer, out int inBufferOffset);
            int rawGot = GetRemainingRawFloats(inBuffer, inBufferOffset, needed * SrcChannels);

            if (rawGot > 0)
            {
                int got = resampler.ResampleOut(data, 0, rawGot / SrcChannels, requested, SrcChannels);
                return got * SrcChannels;
            }

            return 0;
        }

        // must implement either (preferably float one)

        private byte[]? bytes;

        protected virtual int GetRemainingRawFloats(float[] data, int offset, int needed)
        {
            if (bytes == null || needed * 4 != bytes.Length)
                bytes = new byte[needed * 4];

            int got = GetRemainingRawBytes(bytes);

            if (got > 0) Buffer.BlockCopy(bytes, 0, data, offset * 4, got);
            return got / 4;
        }

        private float[]? floats;

        protected virtual int GetRemainingRawBytes(byte[] data)
        {
            if (floats == null || data.Length / 4 != floats.Length)
                floats = new float[data.Length / 4];

            int got = GetRemainingRawFloats(floats, 0, floats.Length);

            if (got > 0) Buffer.BlockCopy(floats, 0, data, 0, got * 4);
            return got * 4;
        }
    }
}
