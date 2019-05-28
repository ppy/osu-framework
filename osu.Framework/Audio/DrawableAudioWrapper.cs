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
        /// <summary>
        /// Global volume of this component.
        /// </summary>
        public readonly BindableDouble Volume = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        private readonly IBindable<double> parentVolume = new BindableDouble(1);

        IBindable<double> IAggregateAudioAdjustment.AggregateVolume => CalculatedVolume;
        IBindable<double> IAggregateAudioAdjustment.AggregateBalance => CalculatedBalance;
        IBindable<double> IAggregateAudioAdjustment.AggregateFrequency => CalculatedFrequency;

        protected BindableDouble CalculatedVolume { get; } = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public readonly BindableDouble Balance = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        private readonly IBindable<double> parentBalance = new BindableDouble();

        protected BindableDouble CalculatedBalance { get; } = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        private readonly IBindable<double> parentFrequency = new BindableDouble(1);

        protected BindableDouble CalculatedFrequency { get; } = new BindableDouble(1);

        /// <summary>
        /// Creates a <see cref="Container"/> that will asynchronously load the given <see cref="Drawable"/> with a delay.
        /// </summary>
        /// <param name="content">The <see cref="Drawable"/> to be loaded.</param>
        protected DrawableAudioWrapper(Drawable content)
            : this()
        {
            AddInternal(content);
        }

        protected DrawableAudioWrapper(AdjustableAudioComponent component)
            : this()
        {
            component.AddAdjustment(AdjustableProperty.Volume, CalculatedVolume);
            component.AddAdjustment(AdjustableProperty.Balance, CalculatedBalance);
            component.AddAdjustment(AdjustableProperty.Frequency, CalculatedFrequency);
        }

        private DrawableAudioWrapper()
        {
            Frequency.ValueChanged += updateFrequency;
            Volume.ValueChanged += updateVolume;
            Balance.ValueChanged += updateBalance;
        }

        [BackgroundDependencyLoader(true)]
        private void load(IAggregateAudioAdjustment parentAdjustment)
        {
            if (parentAdjustment != null)
            {
                parentFrequency.ValueChanged += updateFrequency;
                parentVolume.ValueChanged += updateVolume;
                parentBalance.ValueChanged += updateBalance;

                parentFrequency.BindTo(parentAdjustment.AggregateFrequency);
                parentVolume.BindTo(parentAdjustment.AggregateVolume);
                parentBalance.BindTo(parentAdjustment.AggregateBalance);
            }
        }

        private void updateBalance(ValueChangedEvent<double> obj) => CalculatedBalance.Value = Balance.Value + parentBalance.Value;

        private void updateVolume(ValueChangedEvent<double> obj) => CalculatedVolume.Value = Volume.Value * parentVolume.Value;

        private void updateFrequency(ValueChangedEvent<double> obj) => CalculatedFrequency.Value = Frequency.Value * parentFrequency.Value;
    }
}
