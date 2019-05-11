// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample
{
    public class ComponentSampleChannel : ComponentAudioAdjustContainer
    {
        private readonly SampleChannel channel;

        public ComponentSampleChannel(SampleChannel channel)
        {
            this.channel = channel;
        }

        private readonly IBindable<double> sampleVolume = new BindableDouble(1);

        [BackgroundDependencyLoader(true)]
        private void load(IAudioAdjustment parentAdjustment, AudioManager manager)
        {
            sampleVolume.BindTo(manager.VolumeSample);
            sampleVolume.ValueChanged += updateVolume;
            CalculatedVolume.ValueChanged += updateVolume;

            CalculatedFrequency.BindTo(channel.Frequency);
            CalculatedBalance.BindTo(channel.Balance);
        }

        private void updateVolume(ValueChangedEvent<double> obj) => channel.Volume.Value = CalculatedVolume.Value * sampleVolume.Value;

        public void Play() => channel.Play();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            channel.Dispose();
        }
    }
}
