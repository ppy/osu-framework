// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A wrapper which allows audio components (or adjustments) to exist in the draw hierarchy.
    /// </summary>
    [Cached(typeof(IAggregateAudioAdjustment))]
    public abstract class DrawableAudioWrapper : CompositeDrawable, IAggregateAudioAdjustment
    {
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

        private readonly AdjustableAudioComponent component;

        private readonly bool disposeUnderlyingComponentOnDispose;

        private readonly AudioAdjustments adjustments = new AudioAdjustments();

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

            component.BindAdjustments(adjustments);
        }

        [BackgroundDependencyLoader(true)]
        private void load(IAggregateAudioAdjustment parentAdjustment)
        {
            if (parentAdjustment != null)
                adjustments.BindAdjustments(parentAdjustment);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            component?.UnbindAdjustments(adjustments);

            if (disposeUnderlyingComponentOnDispose)
                component?.Dispose();
        }

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => adjustments.AggregateTempo;

        public IBindable<double> GetAggregate(AdjustableProperty type) => adjustments.GetAggregate(type);

        /// <summary>
        /// Smoothly adjusts <see cref="Volume"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public TransformSequence<DrawableAudioWrapper> VolumeTo(double newVolume, double duration = 0, Easing easing = Easing.None) =>
            this.TransformBindableTo(Volume, newVolume, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Balance"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public TransformSequence<DrawableAudioWrapper> BalanceTo(double newBalance, double duration = 0, Easing easing = Easing.None) =>
            this.TransformBindableTo(Balance, newBalance, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Frequency"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public TransformSequence<DrawableAudioWrapper> FrequencyTo(double newFrequency, double duration = 0, Easing easing = Easing.None) =>
            this.TransformBindableTo(Frequency, newFrequency, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Tempo"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public TransformSequence<DrawableAudioWrapper> TempoTo(double newTempo, double duration = 0, Easing easing = Easing.None) =>
            this.TransformBindableTo(Tempo, newTempo, duration, easing);
    }
}
