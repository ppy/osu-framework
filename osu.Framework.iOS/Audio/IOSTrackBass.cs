// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedBass;
using osu.Framework.Audio.Track;
using ObjCRuntime;

namespace osu.Framework.iOS.Audio
{
    public class IOSTrackBass : TrackBass
    {
        private GCHandle pinnedInstance;

        public IOSTrackBass(Stream data, bool quick = false)
            : base(data, quick)
        {
        }

        protected override DataStreamFileProcedures CreateDataStreamFileProcedures(Stream dataStream) => new IOSDataStreamFileProcedures(dataStream);

        protected override void SetUpCompletionEvents(int handle)
        {
            pinnedInstance = GCHandle.Alloc(this, GCHandleType.Pinned);

            Bass.ChannelSetSync(handle, SyncFlags.Stop, 0, stopSyncProcedure, GCHandle.ToIntPtr(pinnedInstance));
            Bass.ChannelSetSync(handle, SyncFlags.End, 0, endSyncProcedure, GCHandle.ToIntPtr(pinnedInstance));
        }

        private static IOSTrackBass getInstance(IntPtr ptr) => GCHandle.FromIntPtr(ptr).Target as IOSTrackBass;

        [MonoPInvokeCallback(typeof(SyncProcedure))]
        private static void stopSyncProcedure(int handle, int channel, int data, IntPtr user) => getInstance(user).RaiseFailed();

        [MonoPInvokeCallback(typeof(SyncProcedure))]
        private static void endSyncProcedure(int handle, int channel, int data, IntPtr user)
        {
            var inst = getInstance(user);
            if (!inst.Looping)
                inst.RaiseCompleted();
        }
    }
}
