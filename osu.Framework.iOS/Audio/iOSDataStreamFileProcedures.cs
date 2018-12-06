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
    public class iOSDataStreamFileProcedures : DataStreamFileProcedures
    {
        public iOSDataStreamFileProcedures(Stream data) : base(data)
        {
        }

        public override FileProcedures BassProcedures => new FileProcedures
        {
            Close = ac_Close,
            Length = ac_Length,
            Read = ac_Read,
            Seek = ac_Seek
        };

        [MonoPInvokeCallback(typeof(FileCloseProcedure))]
        private static void ac_Close(IntPtr user)
        {
            var handle = GCHandle.FromIntPtr(user);
            DataStreamFileProcedures inst = (DataStreamFileProcedures)handle.Target;
            inst.CloseCallback(user);
        }

        [MonoPInvokeCallback(typeof(FileLengthProcedure))]
        private static long ac_Length(IntPtr user)
        {
            var handle = GCHandle.FromIntPtr(user);
            DataStreamFileProcedures inst = (DataStreamFileProcedures)handle.Target;
            return inst.LengthCallback(user);
        }

        [MonoPInvokeCallback(typeof(FileReadProcedure))]
        private static int ac_Read(IntPtr buffer, int length, IntPtr user)
        {
            var handle = GCHandle.FromIntPtr(user);
            DataStreamFileProcedures inst = (DataStreamFileProcedures)handle.Target;
            return inst.ReadCallback(buffer, length, user);
        }

        [MonoPInvokeCallback(typeof(FileSeekProcedure))]
        private static bool ac_Seek(long offset, IntPtr user)
        {
            var handle = GCHandle.FromIntPtr(user);
            DataStreamFileProcedures inst = (DataStreamFileProcedures)handle.Target;
            return inst.SeekCallback(offset, user);
        }
    }
}