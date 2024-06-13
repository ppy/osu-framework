// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using ManagedBass.Fx;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// An <see cref="IAudioMixer"/> effect.
    /// </summary>
    public class AudioEffect
    {
        internal event Action<AudioEffect, IEffectParameter>? EffectUpdated;

        public IEffectParameter Parameters { get; private set; }
        public readonly int Priority;

        /// <summary>
        /// Creates a new <see cref="AudioEffect"/>.
        /// </summary>
        /// <param name="parameters">The effect parameters (e.g. <see cref="BQFParameters"/>).</param>
        /// <param name="priority">The effect priority. Lower values indicate higher priority and negative values are allowed.
        /// When there are multiple effects with the same priority, their ordering depends on the order in which they are added to the <see cref="IAudioMixer"/>.</param>
        public AudioEffect(IEffectParameter parameters, int priority = 0)
        {
            Parameters = parameters;
            Priority = priority;
        }

        /// <summary>
        /// Updates this effect with new parameters.
        /// </summary>
        /// <remarks>Changing the effect type is not allowed.</remarks>
        /// <param name="parameters">The new parameters.</param>
        public void Update(IEffectParameter parameters)
        {
            if (parameters.FXType != Parameters.FXType)
                throw new InvalidOperationException("The audio effect type cannot be changed.");

            Parameters = parameters;

            EffectUpdated?.Invoke(this, parameters);
        }
    }
}
