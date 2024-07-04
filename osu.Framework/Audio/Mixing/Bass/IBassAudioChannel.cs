// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Interface for audio channels that play audio through BASS (<see cref="TrackBass"/>, <see cref="SampleChannelBass"/>, etc).
    /// </summary>
    internal interface IBassAudioChannel : IAudioChannel
    {
        /// <summary>
        /// Whether this channel is still active within the mix.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// The BASS channel handle.
        /// </summary>
        int Handle { get; }

        /// <summary>
        /// Whether the mixer channel is paused. Only set when removed from a <see cref="BassAudioMixer"/>.
        /// </summary>
        bool MixerChannelPaused { get; set; }

        new BassAudioMixer Mixer { get; }
    }
}
