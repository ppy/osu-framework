// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;
using ObjCRuntime;
using osu.Framework.Allocation;
using osu.Framework.Audio.Callbacks;

namespace osu.Framework.iOS.Audio.Callbacks
{
    public class IOSSyncCallback : SyncCallback
    {
        public override SyncProcedure Callback => sync;

        protected override bool ShouldPin => true;

        public IOSSyncCallback(SyncProcedure proc) : base(proc)
        {
        }

        [MonoPInvokeCallback(typeof(SyncProcedure))]
        private static void sync(int handle, int channel, int data, IntPtr user)
        {
            SyncCallback inst = ObjectHandle<SyncCallback>.FromPointer(user);
            inst.Sync(handle, channel, data, user);
        }
    }
}
