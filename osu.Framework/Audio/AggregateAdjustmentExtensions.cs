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

        /// <summary>
        /// Get aggregated stereo volume by decreasing the opponent channel.
        /// </summary>
        /// <param name="adjustment">The audio adjustments to return from.</param>
        /// <returns>Aggregated stereo volume.</returns>
        internal static (double, double) GetAggregatedStereoVolume(this IAggregateAudioAdjustment adjustment)
        {
            double volume = adjustment.AggregateVolume.Value;
            double balance = adjustment.AggregateBalance.Value;
            double balanceAbs = 1.0 - Math.Abs(balance);

            if (balance < 0)
                return (volume, volume * balanceAbs);
            else if (balance > 0)
                return (volume * balanceAbs, volume);

            return (volume, volume);
        }
    }
}
