// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Wrapper on <see cref="FileProcedures" /> to provide functionality in <see cref="BassCallback" />.
    /// Can delegate to a class implementing the <see cref="IFileProcedures" /> interface.
    /// </summary>
    public class FileCallbacks : BassCallback
    {
        public virtual FileProcedures Callbacks => new FileProcedures
        {
            Read = Read,
            Close = Close,
            Length = Length,
            Seek = Seek
        };

        private readonly IFileProcedures implementation;

        private readonly FileProcedures procedures;

        public FileCallbacks(IFileProcedures implementation)
        {
            this.implementation = implementation;
            procedures = null;
        }

        public FileCallbacks(FileProcedures procedures)
        {
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
    }
}
