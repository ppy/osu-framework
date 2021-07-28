// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A no-sound audio mixer to be used for virtual channels.
    /// </summary>
    public class VirtualAudioMixer : AudioMixer
    {
        /// <summary>
        /// Creates a new <see cref="VirtualAudioMixer"/>.
        /// </summary>
        public VirtualAudioMixer()
            : base(null)
        {
        }

        public override BindableList<IEffectParameter> Effects { get; } = new BindableList<IEffectParameter>();

        protected override void AddInternal(IAudioChannel channel)
        {
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
        }
    }
}
