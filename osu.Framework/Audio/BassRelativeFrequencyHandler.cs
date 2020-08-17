// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using ManagedBass;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A helper class for translating relative frequency values to absolute hertz values based on the initial channel frequency.
    /// Also handles zero frequency value by requesting the component to pause the channel and maintain that until it's set back from zero.
    /// </summary>
    internal class BassRelativeFrequencyHandler
    {
        private int channel;
        private float initialFrequency;

        public Action RequestZeroFrequencyPause;
        public Action RequestZeroFrequencyResume;

        public bool ZeroFrequencyPauseRequested { get; private set; }

        public void SetChannel(int c)
        {
            channel = c;
            ZeroFrequencyPauseRequested = false;

            Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out initialFrequency);
        }

        public void SetFrequency(double relativeFrequency)
        {
            Debug.Assert(channel != 0, "Attempting to set frequency without specifying a channel in SetChannel() before, or an invalid one was specified.");

            // http://bass.radio42.com/help/html/ff7623f0-6e9f-6be8-c8a7-17d3a6dc6d51.htm (BASS_ATTRIB_FREQ's description)
            // Above documentation shows the frequency limits which the constants (min_bass_freq, max_bass_freq) came from.
            const int min_bass_freq = 100;
            const int max_bass_freq = 100000;

            int channelFrequency = (int)Math.Clamp(Math.Abs(initialFrequency * relativeFrequency), min_bass_freq, max_bass_freq);
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, channelFrequency);

            // Maintain internal pause on zero frequency due to BASS not supporting them (0 is took for original rate in BASS API)
            if (!ZeroFrequencyPauseRequested && relativeFrequency == 0)
            {
                RequestZeroFrequencyPause?.Invoke();
                ZeroFrequencyPauseRequested = true;
            }
            else if (ZeroFrequencyPauseRequested && relativeFrequency > 0)
            {
                ZeroFrequencyPauseRequested = false;
                RequestZeroFrequencyResume?.Invoke();
            }
        }
    }
}
