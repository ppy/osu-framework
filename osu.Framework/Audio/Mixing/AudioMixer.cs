// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Abstract class for an audio mixer.
    /// </summary>
    public abstract class AudioMixer : AdjustableAudioComponent, IAudioMixer
    {
        private readonly AudioManager audioManager;

        protected AudioMixer(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        // To always maintain a stable order of events, enqueue actions to the global mixer.
        public void Add(IAudioChannel channel) => audioManager?.Mixer.EnqueueAction(() =>
        {
            if (channel.Mixer == this)
                return;

            channel.Mixer.Remove(channel);
            AddInternal(channel);
            channel.SetMixer(this);
        });

        // To always maintain a stable order of events, enqueue actions to the global mixer.
        public void Remove(IAudioChannel channel) => audioManager?.Mixer.EnqueueAction(() =>
        {
            if (channel.Mixer != this)
                return;

            RemoveInternal(channel);
            channel.SetMixer(null);
        });

        public abstract void AddEffect(IEffectParameter effect, int priority);

        public abstract void RemoveEffect(IEffectParameter effect);

        protected abstract void AddInternal(IAudioChannel channel);

        protected abstract void RemoveInternal(IAudioChannel channel);
    }
}
