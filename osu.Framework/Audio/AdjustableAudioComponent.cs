// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    public class AdjustableAudioComponent : AudioComponent
    {
        /// <summary>
        /// Global volume of this component.
        /// </summary>
        public readonly BindableDouble Volume = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        protected readonly AggregateBindable<double> VolumeAggregate = new AggregateBindable<double>((a, b) => a * b, new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        });

        /// <summary>
        /// Playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public readonly BindableDouble Balance = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        protected readonly AggregateBindable<double> BalanceAggregate = new AggregateBindable<double>((a, b) => a + b, new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        });

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        protected readonly AggregateBindable<double> FrequencyAggregate = new AggregateBindable<double>((a, b) => a * b, new BindableDouble(1));

        internal void InvalidateState(ValueChangedEvent<double> valueChangedEvent = null) => EnqueueAction(OnStateChanged);

        internal virtual void OnStateChanged()
        {
        }

        protected AdjustableAudioComponent()
        {
            VolumeAggregate.AddSource(Volume);
            BalanceAggregate.AddSource(Balance);
            FrequencyAggregate.AddSource(Frequency);

            VolumeAggregate.Result.ValueChanged += InvalidateState;
            BalanceAggregate.Result.ValueChanged += InvalidateState;
            FrequencyAggregate.Result.ValueChanged += InvalidateState;
        }

        public void AddAdjustmentDependency(AdjustableAudioComponent component)
        {
            VolumeAggregate.AddSource(component.VolumeAggregate.Result);
            BalanceAggregate.AddSource(component.BalanceAggregate.Result);
            FrequencyAggregate.AddSource(component.FrequencyAggregate.Result);
        }

        public void RemoveAdjustmentDependency(AdjustableAudioComponent component)
        {
            VolumeAggregate.RemoveSource(component.VolumeAggregate.Result);
            BalanceAggregate.RemoveSource(component.BalanceAggregate.Result);
            FrequencyAggregate.RemoveSource(component.FrequencyAggregate.Result);
        }

        public void AddAdjustment(AdjustableProperty type, BindableDouble adjustBindable) => EnqueueAction(() =>
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    BalanceAggregate.AddSource(adjustBindable);
                    break;

                case AdjustableProperty.Frequency:
                    FrequencyAggregate.AddSource(adjustBindable);
                    break;

                case AdjustableProperty.Volume:
                    VolumeAggregate.AddSource(adjustBindable);
                    break;
            }
        });

        public void RemoveAdjustment(AdjustableProperty type, BindableDouble adjustBindable) => EnqueueAction(() =>
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    BalanceAggregate.RemoveSource(adjustBindable);
                    break;

                case AdjustableProperty.Frequency:
                    FrequencyAggregate.RemoveSource(adjustBindable);
                    break;

                case AdjustableProperty.Volume:
                    VolumeAggregate.RemoveSource(adjustBindable);
                    break;
            }
        });
    }

    public enum AdjustableProperty
    {
        Volume,
        Balance,
        Frequency
    }
}
