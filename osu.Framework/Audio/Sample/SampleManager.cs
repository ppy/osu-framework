//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Sample
{
    public class SampleManager : AudioCollectionManager<AudioSample>
    {
        IResourceStore<byte[]> store;

        public SampleManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public AudioSample Get(string name)
        {
            byte[] data = store.Get(name);

            if (data == null) return null;

            AudioSample sample = new AudioSampleBass(data);
            AddItem(sample);
            return sample;
        }
    }
}
