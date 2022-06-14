// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A wrapper which allows audio components (or adjustments) to exist in the draw hierarchy.
    /// </summary>
    [Cached(typeof(IAggregateAudioAdjustment))]
    public abstract class DrawableAudioWrapper : CompositeDrawable, IAdjustableAudioComponent
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

        public void BindAdjustments(IAggregateAudioAdjustment component) => adjustments.BindAdjustments(component);

        public void UnbindAdjustments(IAggregateAudioAdjustment component) => adjustments.UnbindAdjustments(component);

        private readonly IAdjustableAudioComponent component;

        private readonly bool disposeUnderlyingComponentOnDispose;

        private readonly AudioAdjustments adjustments = new AudioAdjustments();

        private IAggregateAudioAdjustment parentAdjustment;
        private IAudioMixer parentMixer;

        private readonly LayoutValue fromParentLayout = new LayoutValue(Invalidation.Parent);

        private DrawableAudioWrapper()
        {
            AddLayout(fromParentLayout);
        }

        /// <summary>
        /// Creates a <see cref="DrawableAudioWrapper"/> that will contain a drawable child.
        /// Generally used to add adjustments to a hierarchy without adding an audio component.
        /// </summary>
        /// <param name="content">The <see cref="Drawable"/> to be wrapped.</param>
        protected DrawableAudioWrapper(Drawable content)
            : this()
        {
            AddInternal(content);
        }

        /// <summary>
        /// Creates a <see cref="DrawableAudioWrapper"/> that will wrap an audio component (and contain no drawable content).
        /// </summary>
        /// <param name="component">The audio component to wrap.</param>
        /// <param name="disposeUnderlyingComponentOnDispose">Whether the component should be automatically disposed on drawable disposal/expiry.</param>
        protected DrawableAudioWrapper([NotNull] IAdjustableAudioComponent component, bool disposeUnderlyingComponentOnDispose = true)
            : this()
        {
            this.component = component ?? throw new ArgumentNullException(nameof(component));
            this.disposeUnderlyingComponentOnDispose = disposeUnderlyingComponentOnDispose;

            component.BindAdjustments(adjustments);
        }

        protected override void Update()
        {
            base.Update();

            if (!fromParentLayout.IsValid)
            {
                refreshLayoutFromParent();
                fromParentLayout.Validate();
            }
        }

        private void refreshLayoutFromParent()
        {
            // because these components may be pooled, relying on DI is not feasible.
            // in the majority of cases the traversal should be quite short. may require later attention if a use case comes up which this is not true for.
            Drawable cursor = this;
            IAggregateAudioAdjustment newAdjustments = null;
            IAudioMixer newMixer = null;

            while ((cursor = cursor.Parent) != null)
            {
                if (newAdjustments == null && cursor is IAggregateAudioAdjustment candidateAdjustment)
                {
                    // components may be delegating the aggregates of a contained child.
                    // to avoid binding to one's self, check reference equality on an arbitrary bindable.
                    if (candidateAdjustment.AggregateVolume != adjustments.AggregateVolume)
                        newAdjustments = candidateAdjustment;
                }

                if (newMixer == null && cursor is IAudioMixer candidateMixer)
                    newMixer = candidateMixer;

                if (newAdjustments != null && newMixer != null)
                    break;
            }

            if (newAdjustments != parentAdjustment)
            {
                if (parentAdjustment != null) adjustments.UnbindAdjustments(parentAdjustment);
                parentAdjustment = newAdjustments;
                if (parentAdjustment != null) adjustments.BindAdjustments(parentAdjustment);
            }

            if (parentMixer != newMixer)
                OnMixerChanged(new ValueChangedEvent<IAudioMixer>(parentMixer, newMixer));

            parentMixer = newMixer;
        }

        protected virtual void OnMixerChanged(ValueChangedEvent<IAudioMixer> mixer)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            component?.UnbindAdjustments(adjustments);

            if (disposeUnderlyingComponentOnDispose)
                (component as IDisposable)?.Dispose();

            parentAdjustment = null;
            parentMixer = null;
        }

        public void AddAdjustment(AdjustableProperty type, IBindable<double> adjustBindable)
            => adjustments.AddAdjustment(type, adjustBindable);

        public void RemoveAdjustment(AdjustableProperty type, IBindable<double> adjustBindable)
            => adjustments.RemoveAdjustment(type, adjustBindable);

        public void RemoveAllAdjustments(AdjustableProperty type) => adjustments.RemoveAllAdjustments(type);

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;

        public IBindable<double> AggregateTempo => adjustments.AggregateTempo;
    }
}
