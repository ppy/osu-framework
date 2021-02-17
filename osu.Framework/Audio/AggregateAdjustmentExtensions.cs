// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    public static class AggregateAdjustmentExtensions
    {
        /// <summary>
        /// Get the aggregate result for a specific adjustment type.
        /// </summary>
        /// <param name="adjustment">The audio adjustments to return from.</param>
        /// <param name="type">The adjustment type.</param>
        /// <returns>The aggregate result.</returns>
        public static IBindable<double> GetAggregate(this IAggregateAudioAdjustment adjustment, AdjustableProperty type)
        {
            switch (type)
            {
                case AdjustableProperty.Volume:
                    return adjustment.AggregateVolume;

                case AdjustableProperty.Balance:
                    return adjustment.AggregateBalance;

                case AdjustableProperty.Frequency:
                    return adjustment.AggregateFrequency;

                case AdjustableProperty.Tempo:
                    return adjustment.AggregateTempo;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), "Invalid adjustable property type.");
            }
        }
    }
}
