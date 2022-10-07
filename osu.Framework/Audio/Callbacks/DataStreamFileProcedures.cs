// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Implementation of <see cref="IFileProcedures"/> that supports reading from a <see cref="Stream"/>.
    /// </summary>
    public class DataStreamFileProcedures : IFileProcedures
    {
        private readonly Stream dataStream;

        public DataStreamFileProcedures(Stream data)
        {
            dataStream = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void Close(IntPtr user)
        {
        }

        public long Length(IntPtr user)
        {
            if (!dataStream.CanSeek) return 0;

            try
            {
                return dataStream.Length;
            }
            catch
            {
                return 0;
            }
        }

        public unsafe int Read(IntPtr buffer, int length, IntPtr user)
        {
            if (!dataStream.CanRead) return 0;

            try
            {
                return dataStream.Read(new Span<byte>((void*)buffer, length));
            }
            catch
            {
                return 0;
            }
        }

        public bool Seek(long offset, IntPtr user)
        {
            if (!dataStream.CanSeek) return false;

            try
            {
                return dataStream.Seek(offset, SeekOrigin.Begin) == offset;
            }
            catch
            {
                return false;
            }
        }
    }
}
