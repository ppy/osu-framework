// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// An audio component which allows for basic bindable adjustments to be applied.
    /// </summary>
    public class AdjustableAudioComponent : AudioComponent, IAdjustableAudioComponent
    {
        private readonly AudioAdjustments adjustments = new AudioAdjustments();

        /// <summary>
        /// The volume of this component.
        /// </summary>
        public BindableNumber<double> Volume => adjustments.Volume;

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public BindableNumber<double> Balance => adjustments.Balance;

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public BindableNumber<double> Frequency => adjustments.Frequency;

        /// <summary>
        /// Rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        public BindableNumber<double> Tempo => adjustments.Tempo;

        protected AdjustableAudioComponent()
        {
            AggregateVolume.ValueChanged += InvalidateState;
            AggregateBalance.ValueChanged += InvalidateState;
            AggregateFrequency.ValueChanged += InvalidateState;
            AggregateTempo.ValueChanged += InvalidateState;
        }

        public void AddAdjustment(AdjustableProperty type, IBindable<double> adjustBindable) =>
            adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, IBindable<double> adjustBindable) =>
            adjustments.RemoveAdjustment(type, adjustBindable);

        public void RemoveAllAdjustments(AdjustableProperty type) => adjustments.RemoveAllAdjustments(type);

        private bool invalidationPending;

        internal void InvalidateState(ValueChangedEvent<double> valueChangedEvent = null)
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

        public void BindAdjustments(IAggregateAudioAdjustment component) => adjustments.BindAdjustments(component);

        public void UnbindAdjustments(IAggregateAudioAdjustment component) => adjustments.UnbindAdjustments(component);

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => adjustments.AggregateTempo;

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
