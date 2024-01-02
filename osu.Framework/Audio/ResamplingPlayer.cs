// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        internal readonly int SrcRate;
        internal readonly byte SrcChannels;

        /// <summary>
        /// Creates a new <see cref="ResamplingPlayer"/>.
        /// </summary>
        /// <param name="srcRate">Sampling rate of audio that's given from <see cref="GetRemainingRawFloats(float[], int, int)"/></param>
        /// <param name="srcChannels">Channels of audio that's given from <see cref="GetRemainingRawFloats(float[], int, int)"/></param>
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

        public virtual void Clear()
        {
            resampler?.Reset();
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

        protected abstract int GetRemainingRawFloats(float[] data, int offset, int needed);
    }
}
