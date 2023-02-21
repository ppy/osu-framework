// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// An audio component which allows for basic bindable adjustments to be applied.
    /// </summary>
    public class AdjustableAudioComponent : AudioComponent, IAdjustableAudioComponent
    {
        private static readonly object adjustments_acquisition_lock = new object();

        private volatile AudioAdjustments? adjustments;

        protected internal AudioAdjustments Adjustments
        {
            get
            {
                if (adjustments != null)
                    return adjustments;

                lock (adjustments_acquisition_lock)
                {
                    if (adjustments != null)
                        return adjustments;

                    var adj = new AudioAdjustments();

                    adj.AggregateVolume.ValueChanged += InvalidateState;
                    adj.AggregateBalance.ValueChanged += InvalidateState;
                    adj.AggregateFrequency.ValueChanged += InvalidateState;
                    adj.AggregateTempo.ValueChanged += InvalidateState;

                    adjustments = adj;
                }

                return adjustments;
            }
        }

        /// <summary>
        /// The volume of this component.
        /// </summary>
        public BindableNumber<double> Volume => Adjustments.Volume;

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public BindableNumber<double> Balance => Adjustments.Balance;

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public BindableNumber<double> Frequency => Adjustments.Frequency;

        /// <summary>
        /// Rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        public BindableNumber<double> Tempo => Adjustments.Tempo;

        public void AddAdjustment(AdjustableProperty type, IBindable<double> adjustBindable) =>
            Adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, IBindable<double> adjustBindable) =>
            Adjustments.RemoveAdjustment(type, adjustBindable);

        public void RemoveAllAdjustments(AdjustableProperty type) => Adjustments.RemoveAllAdjustments(type);

        private bool invalidationPending;

        internal void InvalidateState(ValueChangedEvent<double>? valueChangedEvent = null)
        {
            if (CanPerformInline)
                OnStateChanged();
            else
                invalidationPending = true;
        }

        internal virtual void OnStateChanged()
        {
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            if (invalidationPending)
            {
                invalidationPending = false;
                OnStateChanged();
            }
        }

        public void BindAdjustments(IAggregateAudioAdjustment component)
        {
            Adjustments.BindAdjustments(component);
        }

        public void UnbindAdjustments(IAggregateAudioAdjustment component) => adjustments?.UnbindAdjustments(component);

        public IBindable<double> AggregateVolume => Adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => Adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => Adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => Adjustments.AggregateTempo;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            AggregateVolume.UnbindAll();
            AggregateBalance.UnbindAll();
            AggregateFrequency.UnbindAll();
            AggregateTempo.UnbindAll();
        }
    }

    public enum AdjustableProperty
    {
        Volume,
        Balance,
        Frequency,
        Tempo
    }
}
