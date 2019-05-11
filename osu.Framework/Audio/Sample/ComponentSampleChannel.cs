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

        [BackgroundDependencyLoader(true)]
        private void load(IAudioAdjustment parentAdjustment)
        {
            ((IBindable<double>)channel.Volume).BindTo(Volume);
            ((IBindable<double>)channel.Balance).BindTo(Balance);
            ((IBindable<double>)channel.Frequency).BindTo(Frequency);
        }

        public void Play() => channel.Play();
    }
}
