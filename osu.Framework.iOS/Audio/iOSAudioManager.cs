// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;

namespace osu.Framework.iOS.Audio
{
    public class IOSAudioManager : AudioManager
    {
        public IOSAudioManager(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore) : base(trackStore, sampleStore)
        {
        }

        protected override TrackManager CreateTrackManager(ResourceStore<byte[]> store) => new IOSTrackManager(store);

        protected override SampleManager CreateSampleManager(IResourceStore<byte[]> store) => new IOSSampleManager(store);
    }
}
