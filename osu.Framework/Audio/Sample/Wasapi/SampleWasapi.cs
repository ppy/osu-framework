using osu.Framework.Audio.Mixing.Wasapi;

namespace osu.Framework.Audio.Sample.Wasapi
{
    internal sealed class SampleWasapi : Sample
    {
        private readonly SampleWasapiFactory factory;
        private readonly WasapiAudioMixer mixer;

        public override bool IsLoaded => true;

        public override double Length => factory.Length;

        internal SampleWasapi(SampleWasapiFactory factory, WasapiAudioMixer mixer)
            : base(factory.Name)
        {
            this.factory = factory;
            this.mixer = mixer;

            PlaybackConcurrency.BindTo(factory.PlaybackConcurrency);
        }

        protected override SampleChannel CreateChannel()
        {
            var channel = new SampleChannelWasapi(Name);
            mixer.Add(channel);
            return channel;
        }
    }
}
