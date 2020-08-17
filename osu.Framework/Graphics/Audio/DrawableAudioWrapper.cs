// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A wrapper which allows audio components (or adjustments) to exist in the draw hierarchy.
    /// </summary>
    [Cached(typeof(IAggregateAudioAdjustment))]
    public abstract class DrawableAudioWrapper : CompositeDrawable, IAggregateAudioAdjustment, IAdjustableAudioComponent
    {
        private readonly AdjustableAudioComponent component;
        private readonly bool disposeUnderlyingComponentOnDispose;

        private readonly AudioWrapperAdjustments wrapperAdjustments = new AudioWrapperAdjustments();
        private AudioAdjustments publicAdjustments => wrapperAdjustments.PublicAdjustments;

        /// <summary>
        /// Creates a <see cref="DrawableAudioWrapper"/> that will contain a drawable child.
        /// Generally used to add adjustments to a hierarchy without adding an audio component.
        /// </summary>
        /// <param name="content">The <see cref="Drawable"/> to be wrapped.</param>
        protected DrawableAudioWrapper(Drawable content)
        {
            AddInternal(content);
        }

        /// <summary>
        /// Creates a <see cref="DrawableAudioWrapper"/> that will wrap an audio component (and contain no drawable content).
        /// </summary>
        /// <param name="component">The audio component to wrap.</param>
        /// <param name="disposeUnderlyingComponentOnDispose">Whether the component should be automatically disposed on drawable disposal/expiry.</param>
        protected DrawableAudioWrapper([NotNull] AdjustableAudioComponent component, bool disposeUnderlyingComponentOnDispose = true)
        {
            this.component = component ?? throw new ArgumentNullException(nameof(component));
            this.disposeUnderlyingComponentOnDispose = disposeUnderlyingComponentOnDispose;

            component.BindAdjustments(wrapperAdjustments);
        }

        [BackgroundDependencyLoader(true)]
        private void load(IAggregateAudioAdjustment parentAdjustment)
        {
            wrapperAdjustments.UpdateClockState(Clock.IsRunning);

            if (parentAdjustment != null)
                publicAdjustments.BindAdjustments(parentAdjustment);
        }

        protected override void Update()
        {
            base.Update();

            // todo: IClock.IsRunning should be a bindable to allow listening for its changes from inside the wrapper adjustments rather than updating per-frame.
            wrapperAdjustments.UpdateClockState(Clock.IsRunning);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            component?.UnbindAdjustments(wrapperAdjustments);

            if (disposeUnderlyingComponentOnDispose)
                component?.Dispose();
        }

        #region Public audio adjustments exposure

        public IBindable<double> AggregateVolume => publicAdjustments.AggregateVolume;
        public IBindable<double> AggregateBalance => publicAdjustments.AggregateBalance;
        public IBindable<double> AggregateFrequency => publicAdjustments.AggregateFrequency;
        public IBindable<double> AggregateTempo => publicAdjustments.AggregateTempo;

        public BindableNumber<double> Volume => publicAdjustments.Volume;
        public BindableNumber<double> Balance => publicAdjustments.Balance;
        public BindableNumber<double> Frequency => publicAdjustments.Frequency;
        public BindableNumber<double> Tempo => publicAdjustments.Tempo;

        public void AddAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable) => publicAdjustments.AddAdjustment(type, adjustBindable);
        public void RemoveAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable) => publicAdjustments.RemoveAdjustment(type, adjustBindable);
        public void RemoveAllAdjustments(AdjustableProperty type) => publicAdjustments.RemoveAllAdjustments(type);

        #endregion

        /// <summary>
        /// Represents a <see cref="AudioAdjustments"/> class with internal adjustments that isn't exposed in <see cref="PublicAdjustments"/>.
        /// </summary>
        private class AudioWrapperAdjustments : IAggregateAudioAdjustment
        {
            /// <summary>
            /// The public adjustments, exposed to consumers for adding adjustments to it.
            /// </summary>
            public readonly AudioAdjustments PublicAdjustments = new AudioAdjustments();

            private readonly AggregateBindable<double> aggregateFrequency;

            IBindable<double> IAggregateAudioAdjustment.AggregateVolume => PublicAdjustments.AggregateVolume;
            IBindable<double> IAggregateAudioAdjustment.AggregateBalance => PublicAdjustments.AggregateBalance;
            IBindable<double> IAggregateAudioAdjustment.AggregateFrequency => aggregateFrequency.Result;
            IBindable<double> IAggregateAudioAdjustment.AggregateTempo => PublicAdjustments.AggregateTempo;

            public AudioWrapperAdjustments()
            {
                aggregateFrequency = new AggregateBindable<double>(AudioAdjustments.GetAggregateFunction(AdjustableProperty.Frequency), PublicAdjustments.Frequency.GetUnboundCopy());
                aggregateFrequency.AddSource(PublicAdjustments.AggregateFrequency);
            }

            private BindableDouble clockRunningFreqAdjust;

            public void UpdateClockState(bool running)
            {
                double adjustValue = running ? 1f : 0f;

                if (clockRunningFreqAdjust == null)
                {
                    clockRunningFreqAdjust = new BindableDouble(adjustValue);
                    aggregateFrequency.AddSource(clockRunningFreqAdjust);
                    return;
                }

                clockRunningFreqAdjust.Value = adjustValue;
            }
        }
    }
}
