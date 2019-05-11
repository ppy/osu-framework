// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Audio
{
    [Cached(typeof(IAudioAdjustment))]
    public class ComponentAudioAdjustContainer : Container, IAudioAdjustment
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

        IBindable<double> IAudioAdjustment.Volume => calculatedVolume;
        IBindable<double> IAudioAdjustment.Balance => calculatedBalance;
        IBindable<double> IAudioAdjustment.Frequency => calculatedFrequency;

        private IBindable<double> calculatedVolume { get; } = new BindableDouble(1)
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

        private IBindable<double> calculatedBalance { get; } = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        private readonly IBindable<double> parentFrequency = new BindableDouble(1);

        private IBindable<double> calculatedFrequency { get; } = new BindableDouble(1);

        [BackgroundDependencyLoader(true)]
        private void load(IAudioAdjustment parentAdjustment)
        {
            if (parentAdjustment != null)
            {
                parentFrequency.BindTo(parentAdjustment.Frequency);
                parentVolume.BindTo(parentAdjustment.Volume);
                parentBalance.BindTo(parentAdjustment.Balance);
            }

            parentFrequency.ValueChanged += updateFrequency;
            Frequency.ValueChanged += updateFrequency;

            parentVolume.ValueChanged += updateVolume;
            Volume.ValueChanged += updateVolume;

            parentBalance.ValueChanged += updateBalance;
            Balance.ValueChanged += updateBalance;
        }

        private void updateBalance(ValueChangedEvent<double> obj) => ((Bindable<double>)calculatedBalance).Value = Balance.Value + parentBalance.Value;

        private void updateVolume(ValueChangedEvent<double> obj) => ((Bindable<double>)calculatedVolume).Value = Volume.Value * parentVolume.Value;

        private void updateFrequency(ValueChangedEvent<double> obj) => ((Bindable<double>)calculatedFrequency).Value = Frequency.Value * parentFrequency.Value;
    }
}
