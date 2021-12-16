// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Threading.Tasks;
using ManagedBass;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/>s into one output.
    /// </summary>
    public abstract class AudioMixer : AudioCollectionManager<AudioComponent>, IAudioMixer, IAudioChannel
    {
        public readonly string Identifier;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        protected AudioMixer(string identifier)
        {
            Identifier = identifier;
        }

        public abstract BindableList<IEffectParameter> Effects { get; }

        internal abstract BindableList<IAudioChannel> Channels { get; }

        public void Add(IAudioChannel channel)
        {
            if (channel is AudioMixer audioMixer)
                AddItem(audioMixer);

            channel.EnqueueAction(() =>
            {
                if (channel.Mixer == this)
                    return;

                // Ensure the channel is removed from its current mixer.
                channel.Mixer?.Remove(channel);

                AddInternal(channel);

                channel.Mixer = this;
            });
        }

        /// <summary>
        /// Removes an <see cref="IAudioChannel"/> from the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to remove.</param>
        public void Remove(IAudioChannel channel)
        {
            channel.EnqueueAction(() =>
            {
                if (channel.Mixer != this)
                    return;

                RemoveInternal(channel);
                channel.Mixer = null;
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

        #region IAudioChannel

        public virtual AudioMixer? Mixer { get; set; }

        Task IAudioChannel.EnqueueAction(Action action) => EnqueueAction(action);

        #endregion
    }
}
