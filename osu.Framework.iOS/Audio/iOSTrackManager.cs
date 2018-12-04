using System;
using System.IO;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;

namespace osu.Framework.iOS.Audio
{
    public class iOSTrackManager : TrackManager
    {
        public iOSTrackManager(IResourceStore<byte[]> store) : base(store)
        {
        }

        protected override TrackBass CreateTrackBass(Stream data, bool quick) => new iOSTrackBass(data, quick);
    }
}
