// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// An audio component which allows for basic bindable adjustments to be applied.
    /// </summary>
    public class AdjustableAudioComponent : AudioComponent
    {
        /// <summary>
        /// The volume of this component.
        /// </summary>
        public readonly BindableDouble Volume = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public readonly BindableDouble Balance = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        protected readonly AggregateBindable<double> VolumeAggregate;
        protected readonly AggregateBindable<double> BalanceAggregate;
        protected readonly AggregateBindable<double> FrequencyAggregate;

        protected AdjustableAudioComponent()
        {
            VolumeAggregate = new AggregateBindable<double>((a, b) => a * b, Volume.GetUnboundCopy());
            VolumeAggregate.AddSource(Volume);
            VolumeAggregate.Result.ValueChanged += InvalidateState;

            BalanceAggregate = new AggregateBindable<double>((a, b) => a + b, Balance.GetUnboundCopy());
            BalanceAggregate.AddSource(Balance);
            BalanceAggregate.Result.ValueChanged += InvalidateState;

            FrequencyAggregate = new AggregateBindable<double>((a, b) => a * b, Frequency.GetUnboundCopy());
            FrequencyAggregate.AddSource(Frequency);
            FrequencyAggregate.Result.ValueChanged += InvalidateState;
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

        internal void InvalidateState(ValueChangedEvent<double> valueChangedEvent = null) => EnqueueAction(OnStateChanged);

        internal virtual void OnStateChanged()
        {
        }

        /// <summary>
        /// Bind all adjustments to another component's aggregated results.
        /// </summary>
        /// <param name="component">The other component (generally a direct parent).</param>
        internal void BindAdjustments(AdjustableAudioComponent component)
        {
            VolumeAggregate.AddSource(component.VolumeAggregate.Result);
            BalanceAggregate.AddSource(component.BalanceAggregate.Result);
            FrequencyAggregate.AddSource(component.FrequencyAggregate.Result);
        }

        /// <summary>
        /// Unbind all adjustments from another component's aggregated results.
        /// </summary>
        /// <param name="component">The other component (generally a direct parent).</param>
        internal void UnbindAdjustments(AdjustableAudioComponent component)
        {
            VolumeAggregate.RemoveSource(component.VolumeAggregate.Result);
            BalanceAggregate.RemoveSource(component.BalanceAggregate.Result);
            FrequencyAggregate.RemoveSource(component.FrequencyAggregate.Result);
        }
    }

    public enum AdjustableProperty
    {
        Volume,
        Balance,
        Frequency
    }
}
