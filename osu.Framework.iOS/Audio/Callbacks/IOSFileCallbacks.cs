// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;
using ObjCRuntime;
using osu.Framework.Allocation;
using osu.Framework.Audio.Callbacks;

namespace osu.Framework.iOS.Audio.Callbacks
{
    public class IOSFileCallbacks : FileCallbacks
    {
        public override FileProcedures Callbacks => new FileProcedures
        {
            Read = read,
            Close = close,
            Length = length,
            Seek = seek
        };

        protected override bool ShouldPin => true;

        public IOSFileCallbacks(IFileProcedures implementation) : base(implementation)
        {
        }

        public IOSFileCallbacks(FileProcedures procedures) : base(procedures)
        {
        }

        [MonoPInvokeCallback(typeof(FileLengthProcedure))]
        private static long length(IntPtr user)
        {
            FileCallbacks inst = ObjectHandle<FileCallbacks>.FromPointer(user);
            return inst.Length(user);
        }

        [MonoPInvokeCallback(typeof(FileCloseProcedure))]
        private static void close(IntPtr user)
        {
            FileCallbacks inst = ObjectHandle<FileCallbacks>.FromPointer(user);
            inst.Close(user);
        }

        [MonoPInvokeCallback(typeof(FileSeekProcedure))]
        private static bool seek(long offset, IntPtr user)
        {
            FileCallbacks inst = ObjectHandle<FileCallbacks>.FromPointer(user);
            return inst.Seek(offset, user);
        }

        [MonoPInvokeCallback(typeof(FileReadProcedure))]
        private static int read(IntPtr buffer, int len, IntPtr user)
        {
            FileCallbacks inst = ObjectHandle<FileCallbacks>.FromPointer(user);
            return inst.Read(buffer, len, user);
        }
    }
}
