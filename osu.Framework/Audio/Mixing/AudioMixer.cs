// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/>s into one output.
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

        public abstract void ApplyEffect(IEffectParameter effect, int priority);

        public abstract void RemoveEffect(IEffectParameter effect);

        /// <summary>
        /// Adds an <see cref="IAudioChannel"/> to the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to add.</param>
        protected abstract void AddInternal(IAudioChannel channel);

        /// <summary>
        /// Removes an <see cref="IAudioChannel"/> from the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to remove.</param>
        protected abstract void RemoveInternal(IAudioChannel channel);
    }
}
