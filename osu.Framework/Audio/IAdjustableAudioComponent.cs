// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    public interface IAdjustableAudioComponent
    {
        /// <summary>
        /// The volume of this component.
        /// </summary>
        BindableNumber<double> Volume { get; }

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        BindableNumber<double> Balance { get; }

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        BindableNumber<double> Frequency { get; }

        /// <summary>
        /// Rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        BindableNumber<double> Tempo { get; }

        /// <summary>
        /// Add a bindable adjustment source.
        /// </summary>
        /// <param name="type">The target type for this adjustment.</param>
        /// <param name="adjustBindable">The bindable adjustment.</param>
        void AddAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable);

        /// <summary>
        /// Remove a bindable adjustment source.
        /// </summary>
        /// <param name="type">The target type for this adjustment.</param>
        /// <param name="adjustBindable">The bindable adjustment.</param>
        void RemoveAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable);
    }
}
