// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    public abstract class AudioEffect
    {
        public readonly int Priority;

        public IEffectParameter? EffectParameter { get; set; }

        protected AudioEffect(int priority = 0)
        {
            Priority = priority;
        }

        /// <summary>
        /// Applies its effect to the audio output.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Removes its effect from the audio output.
        /// </summary>
        public abstract void Remove();
    }
}
