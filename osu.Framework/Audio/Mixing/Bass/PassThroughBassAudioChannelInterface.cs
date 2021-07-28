// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Implements the audio channel method interface by basic BASS method invocations.
    /// </summary>
    internal class PassThroughBassAudioChannelInterface : IBassAudioChannelInterface
    {
        public bool PlayChannel(IBassAudioChannel channel) => ManagedBass.Bass.ChannelPlay(channel.Handle);

        public bool PauseChannel(IBassAudioChannel channel) => ManagedBass.Bass.ChannelPause(channel.Handle);

        public void StopChannel(IBassAudioChannel channel) => ManagedBass.Bass.ChannelStop(channel.Handle);

        public PlaybackState ChannelIsActive(IBassAudioChannel channel) => ManagedBass.Bass.ChannelIsActive(channel.Handle);

        public long GetChannelPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes) => ManagedBass.Bass.ChannelGetPosition(channel.Handle, mode);

        public bool SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode = PositionFlags.Bytes) => ManagedBass.Bass.ChannelSetPosition(channel.Handle, pos, mode);
    }
}
