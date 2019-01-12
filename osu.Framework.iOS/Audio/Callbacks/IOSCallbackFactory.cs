// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using ManagedBass;
using osu.Framework.Audio.Callbacks;

namespace osu.Framework.iOS.Audio.Callbacks
{
    public class IOSCallbackFactory : CallbackFactory
    {
        public override FileCallbacks CreateFileCallbacks(IFileProcedures fileProcedures) => new IOSFileCallbacks(fileProcedures);

        public override FileCallbacks CreateFileCallbacks(FileProcedures fileProcedures) => new IOSFileCallbacks(fileProcedures);

        public override SyncCallback CreateSyncCallback(SyncProcedure syncProcedure) => new IOSSyncCallback(syncProcedure);
    }
}
