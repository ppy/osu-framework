// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample
{
    public class DrawableSampleChannel : DrawableAudioWrapper
    {
        private readonly SampleChannel channel;

        public DrawableSampleChannel(SampleChannel channel)
        {
            this.channel = channel;

            CalculatedFrequency.BindTo(channel.Frequency);
            CalculatedBalance.BindTo(channel.Balance);
            CalculatedVolume.BindValueChanged(updateVolume, true);
        }

        private readonly IBindable<double> globalSampleVolume = new BindableDouble(1);

        [BackgroundDependencyLoader(true)]
        private void load(AudioManager manager)
        {
            globalSampleVolume.ValueChanged += updateVolume;
            globalSampleVolume.BindTo(manager.VolumeSample);
        }

        private void updateVolume(ValueChangedEvent<double> obj) => channel.Volume.Value = CalculatedVolume.Value * globalSampleVolume.Value;

        public void Play() => channel.Play();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            channel.Dispose();
        }
    }
}
