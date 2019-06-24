// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Provides adjustable and bindable attributes for an audio component.
    /// Aggregates results as a <see cref="IAggregateAudioAdjustment"/>.
    /// </summary>
    public class AudioAdjustments : IAggregateAudioAdjustment, IAdjustableAudioComponent
    {
        /// <summary>
        /// The volume of this component.
        /// </summary>
        public BindableDouble Volume { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public BindableDouble Balance { get; } = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public BindableDouble Frequency { get; } = new BindableDouble(1);

        public IBindable<double> AggregateVolume => volumeAggregate.Result;
        public IBindable<double> AggregateBalance => balanceAggregate.Result;
        public IBindable<double> AggregateFrequency => frequencyAggregate.Result;

        private readonly AggregateBindable<double> volumeAggregate;
        private readonly AggregateBindable<double> balanceAggregate;
        private readonly AggregateBindable<double> frequencyAggregate;

        public AudioAdjustments()
        {
            volumeAggregate = new AggregateBindable<double>((a, b) => a * b, Volume.GetUnboundCopy());
            volumeAggregate.AddSource(Volume);

            balanceAggregate = new AggregateBindable<double>((a, b) => a + b, Balance.GetUnboundCopy());
            balanceAggregate.AddSource(Balance);

            frequencyAggregate = new AggregateBindable<double>((a, b) => a * b, Frequency.GetUnboundCopy());
            frequencyAggregate.AddSource(Frequency);
        }

        public void AddAdjustment(AdjustableProperty type, BindableDouble adjustBindable)
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    balanceAggregate.AddSource(adjustBindable);
                    break;

                case AdjustableProperty.Frequency:
                    frequencyAggregate.AddSource(adjustBindable);
                    break;

                case AdjustableProperty.Volume:
                    volumeAggregate.AddSource(adjustBindable);
                    break;
            }
        }

        public void RemoveAdjustment(AdjustableProperty type, BindableDouble adjustBindable)
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    balanceAggregate.RemoveSource(adjustBindable);
                    break;

                case AdjustableProperty.Frequency:
                    frequencyAggregate.RemoveSource(adjustBindable);
                    break;

                case AdjustableProperty.Volume:
                    volumeAggregate.RemoveSource(adjustBindable);
                    break;
            }
        }

        /// <summary>
        /// Bind all adjustments from an <see cref="IAggregateAudioAdjustment"/>.
        /// </summary>
        /// <param name="component">The adjustment source.</param>
        internal void BindAdjustments(IAggregateAudioAdjustment component)
        {
            volumeAggregate.AddSource(component.AggregateVolume);
            balanceAggregate.AddSource(component.AggregateBalance);
            frequencyAggregate.AddSource(component.AggregateFrequency);
        }

        /// <summary>
        /// Unbind all adjustments from an <see cref="IAggregateAudioAdjustment"/>.
        /// </summary>
        /// <param name="component">The adjustment source.</param>
        internal void UnbindAdjustments(IAggregateAudioAdjustment component)
        {
            volumeAggregate.RemoveSource(component.AggregateVolume);
            balanceAggregate.RemoveSource(component.AggregateBalance);
            frequencyAggregate.RemoveSource(component.AggregateFrequency);
        }
    }
}
