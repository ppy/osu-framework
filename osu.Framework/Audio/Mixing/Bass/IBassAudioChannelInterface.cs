// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using ManagedBass;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// An interface providing methods applicable to <see cref="IBassAudioChannel"/>s.
    /// </summary>
    internal interface IBassAudioChannelInterface
    {
        /// <summary>
        /// Plays a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelPlay"/>.</remarks>
        /// <param name="channel">The channel to play.</param>
        /// <param name="restart">Restart playback from the beginning?</param>
        /// <returns>
        /// If successful, <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        bool ChannelPlay(IBassAudioChannel channel, bool restart = false);

        /// <summary>
        /// Pauses a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelPause"/>.</remarks>
        /// <param name="channel">The channel to pause.</param>
        /// <returns>
        /// If successful, <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        bool ChannelPause(IBassAudioChannel channel);

        /// <summary>
        /// Stops a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelStop"/>.</remarks>
        /// <param name="channel">The channel to stop.</param>
        /// <returns>
        /// If successful, <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        bool ChannelStop(IBassAudioChannel channel);

        /// <summary>
        /// Checks if a channel is active (playing) or stalled.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelIsActive"/>.</remarks>
        /// <param name="channel">The channel to get the state of.</param>
        /// <returns><see cref="ManagedBass.PlaybackState"/> indicating the state of the channel.</returns>
        PlaybackState ChannelIsActive(IBassAudioChannel channel);

        /// <summary>
        /// Retrieves the playback position of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetPosition"/>.</remarks>
        /// <param name="channel">The channel to retrieve the position of.</param>
        /// <param name="mode">How to retrieve the position.</param>
        /// <returns>
        /// If an error occurs, -1 is returned, use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// If successful, the position is returned.
        /// </returns>
        long ChannelGetPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes);

        /// <summary>
        /// Sets the playback position of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelSetPosition"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to set the position of.</param>
        /// <param name="position">The position, in units determined by the <paramref name="mode"/>.</param>
        /// <param name="mode">How to set the position.</param>
        /// <returns>
        /// If successful, then <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="P:ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        bool ChannelSetPosition(IBassAudioChannel channel, long position, PositionFlags mode = PositionFlags.Bytes);

        /// <summary>
        /// Retrieves the level (peak amplitude) of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetLevel(int, float[], float, LevelRetrievalFlags)"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to get the levels of.</param>
        /// <param name="levels">The array in which the levels are to be returned.</param>
        /// <param name="length">How much data (in seconds) to look at to get the level (limited to 1 second).</param>
        /// <param name="flags">What levels to retrieve.</param>
        /// <returns><c>true</c> if successful, false otherwise.</returns>
        bool ChannelGetLevel(IBassAudioChannel channel, [In, Out] float[] levels, float length, LevelRetrievalFlags flags);

        /// <summary>
        /// Retrieves the immediate sample data (or an FFT representation of it) of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetData(int, float[], int)"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to retrieve the data of.</param>
        /// <param name="buffer">float[] to write the data to.</param>
        /// <param name="length">Number of bytes wanted, and/or <see cref="T:ManagedBass.DataFlags"/>.</param>
        /// <returns>If an error occurs, -1 is returned, use <see cref="P:ManagedBass.Bass.LastError"/> to get the error code.
        /// <para>When requesting FFT data, the number of bytes read from the channel (to perform the FFT) is returned.</para>
        /// <para>When requesting sample data, the number of bytes written to buffer will be returned (not necessarily the same as the number of bytes read when using the <see cref="F:ManagedBass.DataFlags.Float"/> or DataFlags.Fixed flag).</para>
        /// <para>When using the <see cref="F:ManagedBass.DataFlags.Available"/> flag, the number of bytes in the channel's buffer is returned.</para>
        /// </returns>
        int ChannelGetData(IBassAudioChannel channel, [In, Out] float[] buffer, int length);
    }
}
