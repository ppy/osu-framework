// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Implements audio channel
    /// </summary>
    public class PassThroughBassAudioChannelInterface : IBassAudioChannelInterface
    {
        public bool PlayChannel(IBassAudioChannel channel) => Bass.ChannelPlay(channel.Handle);

        public bool PauseChannel(IBassAudioChannel channel) => Bass.ChannelPause(channel.Handle);

        public void StopChannel(IBassAudioChannel channel) => Bass.ChannelStop(channel.Handle);

        public PlaybackState ChannelIsActive(IBassAudioChannel channel) => Bass.ChannelIsActive(channel.Handle);

        public long GetChannelPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes) => Bass.ChannelGetPosition(channel.Handle, mode);

        public bool SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode = PositionFlags.Bytes) => Bass.ChannelSetPosition(channel.Handle, pos, mode);
    }
}
