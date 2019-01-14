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
            var handle = new ObjectHandle<FileCallbacks>(user);
            return handle.Target.Length(user);
        }

        [MonoPInvokeCallback(typeof(FileCloseProcedure))]
        private static void close(IntPtr user)
        {
            var handle = new ObjectHandle<FileCallbacks>(user);
            handle.Target.Close(user);
        }

        [MonoPInvokeCallback(typeof(FileSeekProcedure))]
        private static bool seek(long offset, IntPtr user)
        {
            var handle = new ObjectHandle<FileCallbacks>(user);
            return handle.Target.Seek(offset, user);
        }

        [MonoPInvokeCallback(typeof(FileReadProcedure))]
        private static int read(IntPtr buffer, int len, IntPtr user)
        {
            var handle = new ObjectHandle<FileCallbacks>(user);
            return handle.Target.Read(buffer, len, user);
        }
    }
}
