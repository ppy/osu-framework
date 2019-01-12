// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using ManagedBass;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Creates instances of <see cref="BassCallback" /> implementations.  Should be subclassed if
    /// platform-specific implementations are required.
    /// </summary>
    public class CallbackFactory
    {
        public virtual SyncCallback CreateSyncCallback(SyncProcedure syncProcedure) => new SyncCallback(syncProcedure);

        public virtual FileCallbacks CreateFileCallbacks(IFileProcedures fileProcedures) => new FileCallbacks(fileProcedures);

        public virtual FileCallbacks CreateFileCallbacks(FileProcedures fileProcedures) => new FileCallbacks(fileProcedures);
    }
}
