// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Abstract class for an audio mixer.
    /// </summary>
    public abstract class AudioMixer : AdjustableAudioComponent, IAudioMixer
    {
        private readonly AudioMixer? defaultMixer;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <param name="defaultMixer">The default <see cref="AudioMixer"/>, which <see cref="IAudioChannel"/>s will be moved to if removed from this one.
        /// A <c>null</c> value indicates the default <see cref="AudioMixer"/>.</param>
        protected AudioMixer(AudioMixer? defaultMixer)
        {
            this.defaultMixer = defaultMixer;
        }

        public void Add(IAudioChannel channel)
        {
            channel.EnqueueAction(() =>
            {
                if (channel.Mixer == this)
                    return;

                // Ensure the channel is removed from its current mixer.
                channel.Mixer?.Remove(channel, false);

                AddInternal(channel);

                channel.Mixer = this;
            });
        }

        public void Remove(IAudioChannel channel) => Remove(channel, true);

        protected void Remove(IAudioChannel channel, bool returnToDefault)
        {
            // If this is the default mixer, prevent removal.
            if (returnToDefault && defaultMixer == null)
                return;

            channel.EnqueueAction(() =>
            {
                if (channel.Mixer != this)
                    return;

                RemoveInternal(channel);

                // Add the channel back to the default mixer so audio can always be played.
                if (returnToDefault)
                    defaultMixer.AsNonNull().Add(channel);
            });
        }

        public abstract void AddEffect(IEffectParameter effect, int priority);

        public abstract void RemoveEffect(IEffectParameter effect);

        protected abstract void AddInternal(IAudioChannel channel);

        protected abstract void RemoveInternal(IAudioChannel channel);
    }
}
