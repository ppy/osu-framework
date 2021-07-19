// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Interface for a BASS audio mixer, providing redirects for common BASS methods.
    /// </summary>
    public interface IBassAudioMixer : IAudioMixer
    {
        /// <summary>
        /// Plays a mixed channel.
        /// </summary>
        /// <param name="channel">The channel to play.</param>
        /// <returns>Whether the channel was played successfully.</returns>
        bool PlayChannel(IBassAudioChannel channel);

        /// <summary>
        /// Pauses a mixed channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to pause.</param>
        /// <returns>Whether the channel was paused successfully.</returns>
        bool PauseChannel(IBassAudioChannel channel);

        /// <summary>
        /// Stops a mixed channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to stop.</param>
        void StopChannel(IBassAudioChannel channel);

        /// <summary>
        /// Retrieves the position of a mixed channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to retrieve the position of.</param>
        /// <param name="mode">The mode in which to return the value.</param>
        /// <returns>The channel position.</returns>
        long GetChannelPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes);

        /// <summary>
        /// Sets the position of a mixed channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to set the position of.</param>
        /// <param name="pos">The new position.</param>
        /// <param name="mode">The mode in which to interpret the given position.</param>
        /// <returns>Whether the channel position was set successfully.</returns>
        bool SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode = PositionFlags.Bytes);
    }
}
