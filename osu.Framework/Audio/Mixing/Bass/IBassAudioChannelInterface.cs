// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using ManagedBass;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// An interface providing all methods which an <see cref="IBassAudioChannel"/> may want to use.
    /// </summary>
    internal interface IBassAudioChannelInterface
    {
        /// <summary>
        /// Plays a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to play.</param>
        /// <returns>Whether the channel was played successfully.</returns>
        bool PlayChannel(IBassAudioChannel channel);

        /// <summary>
        /// Pauses a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to pause.</param>
        /// <returns>Whether the channel was paused successfully.</returns>
        bool PauseChannel(IBassAudioChannel channel);

        /// <summary>
        /// Stops a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to stop.</param>
        bool StopChannel(IBassAudioChannel channel);

        /// <summary>
        /// Returns the current playback state of a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to get the playback state of.</param>
        /// <returns>The <see cref="PlaybackState"/>.</returns>
        PlaybackState ChannelIsActive(IBassAudioChannel channel);

        /// <summary>
        /// Retrieves the position of a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to retrieve the position of.</param>
        /// <param name="mode">The mode in which to return the value.</param>
        /// <returns>The channel position.</returns>
        long GetChannelPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes);

        /// <summary>
        /// Sets the position of a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to set the position of.</param>
        /// <param name="pos">The new position.</param>
        /// <param name="mode">The mode in which to interpret the given position.</param>
        /// <returns>Whether the channel position was set successfully.</returns>
        bool SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode = PositionFlags.Bytes);

        /// <summary>
        /// Retrieves the level (peak amplitude) of a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to get the levels of.</param>
        /// <param name="levels">The array in which the levels are to be returned.</param>
        /// <param name="length">How much data (in seconds) to look at to get the level (limited to 1 second).</param>
        /// <param name="flags">What levels to retrieve.</param>
        /// <returns><c>true</c> if successful, false otherwise.</returns>
        bool ChannelGetLevel(IBassAudioChannel channel, [In, Out] float[] levels, float length, LevelRetrievalFlags flags);

        /// <summary>
        /// Retrieves the immediate sample data (or an FFT representation of it) of a channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to retrieve the data of.</param>
        /// <param name="buffer">float[] to write the data to.</param>
        /// <param name="length">Number of bytes wanted, and/or <see cref="T:ManagedBass.DataFlags" />.</param>
        /// <returns>If an error occurs, -1 is returned, use <see cref="P:ManagedBass.Bass.LastError" /> to get the error code.
        /// <para>When requesting FFT data, the number of bytes read from the channel (to perform the FFT) is returned.</para>
        /// <para>When requesting sample data, the number of bytes written to buffer will be returned (not necessarily the same as the number of bytes read when using the <see cref="F:ManagedBass.DataFlags.Float" /> or DataFlags.Fixed flag).</para>
        /// <para>When using the <see cref="F:ManagedBass.DataFlags.Available" /> flag, the number of bytes in the channel's buffer is returned.</para>
        /// </returns>
        int ChannelGetData(IBassAudioChannel channel, [In, Out] float[] buffer, int length);
    }
}
