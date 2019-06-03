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
        public BindableDouble Volume => adjustments.Volume;

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public BindableDouble Balance => adjustments.Balance;

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public BindableDouble Frequency => adjustments.Frequency;

        protected AdjustableAudioComponent()
        {
            AggregateVolume.ValueChanged += InvalidateState;
            AggregateBalance.ValueChanged += InvalidateState;
            AggregateFrequency.ValueChanged += InvalidateState;
        }

        public void AddAdjustment(AdjustableProperty type, BindableDouble adjustBindable) =>
            adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, BindableDouble adjustBindable) =>
            adjustments.RemoveAdjustment(type, adjustBindable);

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            AggregateVolume.UnbindAll();
            AggregateBalance.UnbindAll();
            AggregateFrequency.UnbindAll();
        }
    }

    public enum AdjustableProperty
    {
        Volume,
        Balance,
        Frequency
    }
}
