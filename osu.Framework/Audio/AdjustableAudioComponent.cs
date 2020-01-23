// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    /// <summary>
    /// An audio component which allows for basic bindable adjustments to be applied.
    /// </summary>
    public class AdjustableAudioComponent : AudioComponent, IAggregateAudioAdjustment, IAdjustableAudioComponent
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

        public void AddAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable) =>
            adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable) =>
            adjustments.RemoveAdjustment(type, adjustBindable);

        public void RemoveAllAdjustments(AdjustableProperty type) => adjustments.RemoveAllAdjustments(type);

        internal void InvalidateState(ValueChangedEvent<double> valueChangedEvent = null) => EnqueueAction(OnStateChanged);

        internal virtual void OnStateChanged()
        {
        }

        /// <summary>
        /// Bind all adjustments to another component's aggregated results.
        /// </summary>
        /// <param name="component">The other component (generally a direct parent).</param>
        internal void BindAdjustments(IAggregateAudioAdjustment component) => adjustments.BindAdjustments(component);

        /// <summary>
        /// Unbind all adjustments from another component's aggregated results.
        /// </summary>
        /// <param name="component">The other component (generally a direct parent).</param>
        internal void UnbindAdjustments(IAggregateAudioAdjustment component) => adjustments.UnbindAdjustments(component);

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => adjustments.AggregateTempo;

        public IBindable<double> GetAggregate(AdjustableProperty type) => adjustments.GetAggregate(type);

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
