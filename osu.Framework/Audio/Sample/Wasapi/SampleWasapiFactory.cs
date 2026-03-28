using osu.Framework.Audio.Mixing.Wasapi;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample.Wasapi
{
    internal class SampleWasapiFactory : AudioCollectionManager<AdjustableAudioComponent>
    {
        public string Name { get; }

        public override bool IsLoaded => true;

        public double Length { get; private set; }

        internal readonly Bindable<int> PlaybackConcurrency = new Bindable<int>(Sample.DEFAULT_CONCURRENCY);

        private readonly WasapiAudioMixer mixer;

        public SampleWasapiFactory(byte[] data, string name, WasapiAudioMixer mixer)
        {
            Name = name;
            this.mixer = mixer;

            // Decoding is not implemented in this prototype. Assume loaded.
            Length = 0;
        }

        internal bool CanEvict => Items.Count == 0 && PendingActions.IsEmpty;

        internal override void UpdateDevice(int deviceIndex)
        {
            // No-op for prototype
        }

        public Sample CreateSample() => new SampleWasapi(this, mixer) { OnPlay = onPlay };

        private void onPlay(Sample sample)
        {
            AddItem(sample);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
