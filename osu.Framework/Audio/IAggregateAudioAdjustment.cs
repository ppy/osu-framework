// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Provides aggregated adjustments for an audio component.
    /// </summary>
    public interface IAggregateAudioAdjustment
    {
        /// <summary>
        /// The aggregate volume of this component (0..1).
        /// </summary>
        IBindable<double> AggregateVolume { get; }

        /// <summary>
        /// The aggregate playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        IBindable<double> AggregateBalance { get; }

        /// <summary>
        /// The aggregate rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        IBindable<double> AggregateFrequency { get; }

        /// <summary>
        /// The aggregate rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        IBindable<double> AggregateTempo { get; }
    }
}
