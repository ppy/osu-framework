// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/>s into one output.
    /// </summary>
    public abstract class AudioMixer : AudioComponent, IAudioMixer
    {
        public readonly string Identifier;

        private readonly AudioMixer? globalMixer;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <param name="globalMixer">The global <see cref="AudioMixer"/>, which <see cref="IAudioChannel"/>s are moved to if removed from this one.
        /// A <c>null</c> value indicates this is the global <see cref="AudioMixer"/>.</param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        protected AudioMixer(AudioMixer? globalMixer, string identifier)
        {
            this.globalMixer = globalMixer;
            Identifier = identifier;
        }

        public abstract BindableList<IEffectParameter> Effects { get; }

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

        /// <summary>
        /// Removes an <see cref="IAudioChannel"/> from the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to remove.</param>
        /// <param name="returnToDefault">Whether <paramref name="channel"/> should be returned to the default mixer.</param>
        protected void Remove(IAudioChannel channel, bool returnToDefault)
        {
            // If this is the default mixer, prevent removal.
            if (returnToDefault && globalMixer == null)
                return;

            channel.EnqueueAction(() =>
            {
                if (channel.Mixer != this)
                    return;

                RemoveInternal(channel);
                channel.Mixer = null;

                // Add the channel back to the default mixer so audio can always be played.
                if (returnToDefault)
                    globalMixer.AsNonNull().Add(channel);
            });
        }

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
