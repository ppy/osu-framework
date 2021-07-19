// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A BASS audio mixer which does NOT mix.
    /// </summary>
    public class PassThroughBassAudioMixer : AudioMixer, IBassAudioMixer
    {
        protected override void AddInternal(IAudioChannel channel)
        {
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            StopChannel((IBassAudioChannel)channel);
        }

        void IBassAudioMixer.RegisterChannel(IBassAudioChannel channel)
        {
        }

        public bool PlayChannel(IBassAudioChannel channel) => Bass.ChannelPlay(channel.Handle);

        public bool PauseChannel(IBassAudioChannel channel) => Bass.ChannelPause(channel.Handle);

        public void StopChannel(IBassAudioChannel channel) => Bass.ChannelStop(channel.Handle);

        public long GetChannelPosition(IBassAudioChannel channel, PositionFlags mode) => Bass.ChannelGetPosition(channel.Handle);

        public bool SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode) => Bass.ChannelSetPosition(channel.Handle, pos, mode);
    }
}
