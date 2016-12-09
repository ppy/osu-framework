// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.IO;
using osu.Framework.IO.Stores;

namespace osu.Framework.Audio.Sample
{
    public class SampleManager : AudioCollectionManager<AudioSample>, IResourceStore<AudioSample>
    {
        IResourceStore<byte[]> store;

        Dictionary<string, AudioSample> sampleCache = new Dictionary<string, AudioSample>();

        public SampleManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        public AudioSample Get(string name)
        {
            lock (sampleCache)
            {
                AudioSample sample;
                if (sampleCache.TryGetValue(name, out sample))
                {
                    if (sample == null) return null;

                    //use existing bass sample (but provide a new audiosample instance).
                    sample = new AudioSampleBass(((AudioSampleBass)sample).SampleId);
                }
                else
                {
                    byte[] data = store.Get(name);
                    if (data != null)
                        sample = new AudioSampleBass(data);
                    sampleCache[name] = sample;
                }

                if (sample != null)
                    AddItem(sample);
                return sample;
            }
        }

        public Stream GetStream(string name)
        {
            return store.GetStream(name);
        }
    }
}
