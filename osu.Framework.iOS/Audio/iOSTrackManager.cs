// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
