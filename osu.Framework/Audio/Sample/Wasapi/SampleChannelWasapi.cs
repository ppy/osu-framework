namespace osu.Framework.Audio.Sample.Wasapi
{
    internal sealed class SampleChannelWasapi : SampleChannel
    {
        private bool playing;

        public override bool Playing => playing;

        public SampleChannelWasapi(string name)
            : base(name)
        {
        }

        public override void Play()
        {
            base.Play();
            playing = true;
        }

        public override void Stop()
        {
            base.Stop();
            playing = false;
        }
    }
}
