// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
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

        /// <summary>
        /// Invoked when frequency changes from non-zero to zero via <see cref="SetFrequency"/>.
        /// Allows the component using this instance to pause instead of changing frequency to zero
        /// (which is not supported in BASS).
        /// </summary>
        public Action FrequencyChangedToZero;

        /// <summary>
        /// Invoked when frequency changes from zero to non-zero via <see cref="SetFrequency"/>.
        /// Allows the component using this instance to revert any changes in its state
        /// done in <see cref="FrequencyChangedToZero"/>.
        /// </summary>
        public Action FrequencyChangedFromZero;

        /// <summary>
        /// Whether the last <see cref="SetFrequency"/> call specified a zero relative frequency.
        /// </summary>
        public bool IsFrequencyZero { get; private set; }

        /// <summary>
        /// Sets the component's BASS channel handle.
        /// </summary>
        /// <param name="channel">The channel handle.</param>
        public void SetChannel(int channel)
        {
            if (channel == 0)
                throw new ArgumentException("Invalid channel handle specified.", nameof(channel));

            this.channel = channel;
            IsFrequencyZero = false;

            Bass.ChannelGetAttribute(this.channel, ChannelAttribute.Frequency, out initialFrequency);
        }

        /// <summary>
        /// Sets the channel's frequency based on the given <paramref name="relativeFrequency"/>.
        /// </summary>
        /// <remarks>
        /// Callers should ensure to <see cref="SetChannel"/> first before attempting to change channel frequency.
        /// </remarks>
        /// <param name="relativeFrequency">The desired frequency value, relative to the channel's initial frequency.</param>
        /// <example>
        /// A <c>SetFrequency(0.5)</c> call is equivalent to the following ManagedBASS call:
        /// <code>BASS.ChannelSetAttribute(ChannelAttribute.Frequency, channel, initialFrequency * 0.5);</code>
        /// </example>
        public void SetFrequency(double relativeFrequency)
        {
            if (channel == 0)
                throw new InvalidOperationException("Attempted to set the channel frequency without calling SetChannel() first.");

            // http://bass.radio42.com/help/html/ff7623f0-6e9f-6be8-c8a7-17d3a6dc6d51.htm (BASS_ATTRIB_FREQ's description)
            // Above documentation shows the frequency limits which the constants (min_bass_freq, max_bass_freq) came from.
            const int min_bass_freq = 100;
            const int max_bass_freq = 100000;

            int channelFrequency = (int)Math.Clamp(Math.Abs(initialFrequency * relativeFrequency), min_bass_freq, max_bass_freq);
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, channelFrequency);

            // Maintain internal pause on zero frequency due to BASS not supporting them (0 is took for original rate in BASS API)
            if (!IsFrequencyZero && relativeFrequency == 0)
            {
                FrequencyChangedToZero?.Invoke();
                IsFrequencyZero = true;
            }
            else if (IsFrequencyZero && relativeFrequency > 0)
            {
                IsFrequencyZero = false;
                FrequencyChangedFromZero?.Invoke();
            }
        }
    }
}
