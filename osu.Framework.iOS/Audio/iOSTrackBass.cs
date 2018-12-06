// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedBass;
using ObjCRuntime;
using osu.Framework.Audio.Track;

namespace osu.Framework.iOS.Audio
{
    // ReSharper disable once InconsistentNaming
    public class iOSTrackBass : TrackBass
    {
        public iOSTrackBass(Stream data, bool quick = false) : base(data, quick)
        {
        }

        protected override DataStreamFileProcedures CreateDataStreamFileProcedures(Stream dataStream) => new iOSDataStreamFileProcedures(dataStream);
    }
}