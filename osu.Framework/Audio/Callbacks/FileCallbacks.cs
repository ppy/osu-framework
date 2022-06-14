// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using ManagedBass;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Wrapper on <see cref="FileProcedures"/> to provide functionality in <see cref="BassCallback"/>.
    /// Can delegate to a class implementing the <see cref="IFileProcedures"/> interface.
    /// </summary>
    public class FileCallbacks : BassCallback
    {
        public FileProcedures Callbacks { get; private set; }

        private FileProcedures instanceProcedures => new FileProcedures
        {
            Read = Read,
            Close = Close,
            Length = Length,
            Seek = Seek
        };

        private static readonly FileProcedures static_procedures = new FileProcedures
        {
            Read = readCallback,
            Close = closeCallback,
            Length = lengthCallback,
            Seek = seekCallback
        };

        private readonly IFileProcedures implementation;

        private readonly FileProcedures procedures;

        public FileCallbacks(IFileProcedures implementation)
        {
            Callbacks = RuntimeInfo.SupportsJIT ? instanceProcedures : static_procedures;
            this.implementation = implementation;
            procedures = null;
        }

        public FileCallbacks(FileProcedures procedures)
        {
            Callbacks = RuntimeInfo.SupportsJIT ? instanceProcedures : static_procedures;
            this.procedures = procedures;
            implementation = null;
        }

        public long Length(IntPtr user) => implementation?.Length(user) ?? procedures.Length(user);

        public bool Seek(long offset, IntPtr user) => implementation?.Seek(offset, user) ?? procedures.Seek(offset, user);

        public int Read(IntPtr buffer, int length, IntPtr user) => implementation?.Read(buffer, length, user) ?? procedures.Read(buffer, length, user);

        public void Close(IntPtr user)
        {
            if (implementation != null)
                implementation.Close(user);
            else
                procedures.Close(user);
        }

        [MonoPInvokeCallback(typeof(FileReadProcedure))]
        private static int readCallback(IntPtr buffer, int length, IntPtr user)
        {
            var ptr = new ObjectHandle<FileCallbacks>(user);
            if (ptr.GetTarget(out FileCallbacks target))
                return target.Read(buffer, length, user);

            return 0;
        }

        [MonoPInvokeCallback(typeof(FileCloseProcedure))]
        private static void closeCallback(IntPtr user)
        {
            var ptr = new ObjectHandle<FileCallbacks>(user);
            if (ptr.GetTarget(out FileCallbacks target))
                target.Close(user);
        }

        [MonoPInvokeCallback(typeof(FileLengthProcedure))]
        private static long lengthCallback(IntPtr user)
        {
            var ptr = new ObjectHandle<FileCallbacks>(user);
            if (ptr.GetTarget(out FileCallbacks target))
                return target.Length(user);

            return 0;
        }

        [MonoPInvokeCallback(typeof(FileSeekProcedure))]
        private static bool seekCallback(long offset, IntPtr user)
        {
            var ptr = new ObjectHandle<FileCallbacks>(user);
            if (ptr.GetTarget(out FileCallbacks target))
                return target.Seek(offset, user);

            return false;
        }
    }
}
