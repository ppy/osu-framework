// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using ManagedBass;
using osu.Framework.Allocation;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Wrapper on <see cref="SyncProcedure" /> to provide functionality in <see cref="BassCallback" />.
    /// </summary>
    public class SyncCallback : BassCallback
    {
        public virtual SyncProcedure Callback => Sync;

        public readonly SyncProcedure Sync;

        public SyncCallback(SyncProcedure proc)
        {
            Sync = proc;
        }
    }
}
