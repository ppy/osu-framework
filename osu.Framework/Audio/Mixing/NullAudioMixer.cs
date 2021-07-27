// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A no-sound audio mixer.
    /// </summary>
    public class NullAudioMixer : AudioMixer
    {
        /// <summary>
        /// Creates a new <see cref="NullAudioMixer"/>.
        /// </summary>
        public NullAudioMixer()
            : base(null)
        {
        }

        public override void ApplyEffect(IEffectParameter effect, int priority)
        {
        }

        public override void RemoveEffect(IEffectParameter effect)
        {
        }

        protected override void AddInternal(IAudioChannel channel)
        {
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
        }
    }
}
