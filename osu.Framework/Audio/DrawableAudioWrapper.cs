// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Audio
{
    [Cached(typeof(IAggregateAudioAdjustment))]
    public abstract class DrawableAudioWrapper : CompositeDrawable, IAggregateAudioAdjustment
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

        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// </summary>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        protected DrawableAudioWrapper(Drawable content)
        {
            AddInternal(content);
        }

        protected DrawableAudioWrapper(AdjustableAudioComponent component)
        {
            component.BindAdjustments(adjustments);
        }

        [BackgroundDependencyLoader(true)]
        private void load(IAggregateAudioAdjustment parentAdjustment)
        {
            if (parentAdjustment != null)
                adjustments.BindAdjustments(parentAdjustment);
        }

        public IBindable<double> AggregateVolume => adjustments.AggregateVolume;

        public IBindable<double> AggregateBalance => adjustments.AggregateBalance;

        public IBindable<double> AggregateFrequency => adjustments.AggregateFrequency;
    }
}
