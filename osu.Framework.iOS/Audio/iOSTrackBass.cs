// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.Audio.Track;

namespace osu.Framework.iOS.Audio
{
    public class IOSTrackBass : TrackBass
    {
        public IOSTrackBass(Stream data, bool quick = false) : base(data, quick)
        {
        }

        protected override DataStreamFileProcedures CreateDataStreamFileProcedures(Stream dataStream) => new IOSDataStreamFileProcedures(dataStream);
    }
}