// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Abstract class for an audio mixer.
    /// </summary>
    public abstract class AudioMixer : AdjustableAudioComponent, IAudioMixer
    {
        public void Add(IAudioChannel channel)
        {
            if (channel.Mixer == this)
                return;

            // Ensure the channel is removed from its current mixer.
            channel.Mixer?.Remove(channel);

            AddInternal(channel);
            channel.Mixer = this;
        }

        public void Remove(IAudioChannel channel)
        {
            if (channel.Mixer != this)
                return;

            RemoveInternal(channel);
            channel.Mixer = null;
        }

        public abstract void AddEffect(IEffectParameter effect, int priority);

        public abstract void RemoveEffect(IEffectParameter effect);

        protected abstract void AddInternal(IAudioChannel channel);

        protected abstract void RemoveInternal(IAudioChannel channel);

        internal new Task EnqueueAction(Action action) => base.EnqueueAction(action);
    }
}
