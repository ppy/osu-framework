// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Provides adjustable and bindable attributes for an audio component.
    /// Aggregates results as a <see cref="IAggregateAudioAdjustment"/>.
    /// </summary>
    public class AudioAdjustments : IAdjustableAudioComponent
    {
        private static readonly AdjustableProperty[] all_adjustments = (AdjustableProperty[])Enum.GetValues(typeof(AdjustableProperty));

        /// <summary>
        /// The volume of this component.
        /// </summary>
        public BindableNumber<double> Volume { get; } = new BindableDouble(1)
        {
            Default = 1,
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public BindableNumber<double> Balance { get; } = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public BindableNumber<double> Frequency { get; } = new BindableDouble(1)
        {
            Default = 1,
        };

        /// <summary>
        /// Rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        public BindableNumber<double> Tempo { get; } = new BindableDouble(1)
        {
            Default = 1,
        };

        public IBindable<double> AggregateVolume => volumeAggregate.Result;
        public IBindable<double> AggregateBalance => balanceAggregate.Result;
        public IBindable<double> AggregateFrequency => frequencyAggregate.Result;
        public IBindable<double> AggregateTempo => tempoAggregate.Result;

        private AggregateBindable<double> volumeAggregate;
        private AggregateBindable<double> balanceAggregate;
        private AggregateBindable<double> frequencyAggregate;
        private AggregateBindable<double> tempoAggregate;

        public AudioAdjustments()
        {
            foreach (AdjustableProperty type in all_adjustments)
            {
                var aggregate = getAggregate(type) = new AggregateBindable<double>(getAggregateFunction(type), getProperty(type).GetUnboundCopy());
                aggregate.AddSource(getProperty(type));
            }
        }

        public void AddAdjustment(AdjustableProperty type, IBindable<double> adjustBindable)
            => getAggregate(type).AddSource(adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, IBindable<double> adjustBindable)
            => getAggregate(type).RemoveSource(adjustBindable);

        public void BindAdjustments(IAggregateAudioAdjustment component)
        {
            foreach (AdjustableProperty type in all_adjustments)
                getAggregate(type).AddSource(component.GetAggregate(type));
        }

        public void UnbindAdjustments(IAggregateAudioAdjustment component)
        {
            foreach (AdjustableProperty type in all_adjustments)
                getAggregate(type).RemoveSource(component.GetAggregate(type));
        }

        public void RemoveAllAdjustments(AdjustableProperty type)
        {
            var aggregate = getAggregate(type);

            aggregate.RemoveAllSources();
            aggregate.AddSource(getProperty(type));
        }

        private ref AggregateBindable<double> getAggregate(AdjustableProperty type)
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    return ref balanceAggregate;

                case AdjustableProperty.Frequency:
                    return ref frequencyAggregate;

                case AdjustableProperty.Volume:
                    return ref volumeAggregate;

                case AdjustableProperty.Tempo:
                    return ref tempoAggregate;
            }

            throw new ArgumentException($"{nameof(AdjustableProperty)} \"{type}\" is missing mapping", nameof(type));
        }

        private BindableNumber<double> getProperty(AdjustableProperty type)
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    return Balance;

                case AdjustableProperty.Frequency:
                    return Frequency;

                case AdjustableProperty.Volume:
                    return Volume;

                case AdjustableProperty.Tempo:
                    return Tempo;
            }

            throw new ArgumentException($"{nameof(AdjustableProperty)} \"{type}\" is missing mapping", nameof(type));
        }

        private Func<double, double, double> getAggregateFunction(AdjustableProperty type)
        {
            switch (type)
            {
                default:
                    return (a, b) => a * b;

                case AdjustableProperty.Balance:
                    return (a, b) => a + b;
            }
        }
    }
}
