// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ManagedBass;
using ObjCRuntime;
using osu.Framework.Audio.Sample;

namespace osu.Framework.iOS.Audio
{
    // ReSharper disable once InconsistentNaming
    public class iOSSampleBass : SampleBass
    {
        private GCHandle pinnedData;
        private GCHandle pinnedInstance;

        public iOSSampleBass(byte[] data, ConcurrentQueue<Task> customPendingActions = null, int concurrency = DEFAULT_CONCURRENCY) : base(data, customPendingActions, concurrency)
        {
        }

        protected override int LoadSample(byte[] data)
        {
            pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
            var id = Bass.SampleLoad(pinnedData.AddrOfPinnedObject(), 0, data.Length, PlaybackConcurrency, BassFlags.Default | BassFlags.SampleOverrideLongestPlaying);
            if (id == 0)
                pinnedData.Free();
            else
            {
                pinnedInstance = GCHandle.Alloc(this, GCHandleType.Pinned);
                Bass.ChannelSetSync(id, SyncFlags.Free, 0, syncProcedure, GCHandle.ToIntPtr(pinnedInstance));
            }
            return id;
        }

        protected override void Dispose(bool disposing)
        {
            if (pinnedData.IsAllocated)
                pinnedData.Free();

            if (pinnedInstance.IsAllocated)
                pinnedInstance.Free();

            base.Dispose(disposing);
        }

        [MonoPInvokeCallback(typeof(SyncProcedure))]
        private static void syncProcedure(int handle, int channel, int data, IntPtr user)
        {
            var gcHandle = GCHandle.FromIntPtr(user);
            iOSSampleBass inst = (iOSSampleBass)gcHandle.Target;
            if (inst.pinnedData.IsAllocated)
                inst.pinnedData.Free();
        }
    }
}
