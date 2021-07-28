// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Implements the audio channel method interface through basic BASS method invocations.
    /// </summary>
    internal class PassThroughBassAudioChannelInterface : IBassAudioChannelInterface
    {
        public bool ChannelPlay(IBassAudioChannel channel, bool restart) => ManagedBass.Bass.ChannelPlay(channel.Handle, restart);

        public bool ChannelPause(IBassAudioChannel channel) => ManagedBass.Bass.ChannelPause(channel.Handle);

        public bool ChannelStop(IBassAudioChannel channel) => ManagedBass.Bass.ChannelStop(channel.Handle);

        public PlaybackState ChannelIsActive(IBassAudioChannel channel) => ManagedBass.Bass.ChannelIsActive(channel.Handle);

        public long ChannelGetPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes) => ManagedBass.Bass.ChannelGetPosition(channel.Handle, mode);

        public bool ChannelSetPosition(IBassAudioChannel channel, long position, PositionFlags mode = PositionFlags.Bytes) => ManagedBass.Bass.ChannelSetPosition(channel.Handle, position, mode);

        public bool ChannelGetLevel(IBassAudioChannel channel, float[] levels, float length, LevelRetrievalFlags flags) => ManagedBass.Bass.ChannelGetLevel(channel.Handle, levels, length, flags);

        public int ChannelGetData(IBassAudioChannel channel, float[] buffer, int length) => ManagedBass.Bass.ChannelGetData(channel.Handle, buffer, length);
    }
}
