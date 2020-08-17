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

        public void AddAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable)
            => adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, BindableNumber<double> adjustBindable)
            => adjustments.RemoveAdjustment(type, adjustBindable);

        public void RemoveAllAdjustments(AdjustableProperty type) => adjustments.RemoveAllAdjustments(type);

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => adjustments.AggregateTempo;
    }
}
