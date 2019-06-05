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
    [Cached(typeof(IAggregateAudioAdjustment))]
    public abstract class DrawableAudioWrapper : CompositeDrawable, IAggregateAudioAdjustment
    {
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

        private readonly AdjustableAudioComponent component;

        private readonly bool disposeUnderlyingComponentOnDispose;

        private readonly AudioAdjustments adjustments = new AudioAdjustments();

        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// </summary>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        protected DrawableAudioWrapper(Drawable content)
        {
            AddInternal(content);
        }

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
    }
}
