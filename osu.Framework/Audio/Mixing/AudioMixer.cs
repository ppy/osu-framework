// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Threading.Tasks;
using ManagedBass;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/>s into one output.
    /// </summary>
    public abstract class AudioMixer : AudioCollectionManager<AudioComponent>, IAudioMixer, IAudioChannel
    {
        public readonly string Identifier;

        private readonly AudioMixer? parentMixer;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <param name="parentMixer">The <see cref="AudioMixer"/> to route audio output to. <see cref="IAudioChannel"/>s are moved to this if removed from this one.
        /// A <c>null</c> value indicates this is the global <see cref="AudioMixer"/>.</param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        protected AudioMixer(AudioMixer? parentMixer, string identifier)
        {
            this.parentMixer = parentMixer;
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
        /// <param name="moveToParent">Whether <paramref name="channel"/> should be re-routed to the parent mixer.</param>
        protected void Remove(IAudioChannel channel, bool moveToParent)
        {
            channel.EnqueueAction(() =>
            {
                if (channel.Mixer != this)
                    return;

                RemoveInternal(channel);
                channel.Mixer = null;

                // Move channel to parent mixer if requested (and present).
                if (moveToParent && parentMixer != null && !parentMixer.IsDisposed)
                    parentMixer.AsNonNull().Add(channel);
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

        public AudioMixer? Mixer { get; }

        AudioMixer? IAudioChannel.Mixer { get; set; }

        Task IAudioChannel.EnqueueAction(Action action) => EnqueueAction(action);

        #endregion
    }
}
