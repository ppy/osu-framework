// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Implementation of <see cref="IFileProcedures"/> that supports reading from a <see cref="Stream"/>.
    /// </summary>
    public class DataStreamFileProcedures : IFileProcedures
    {
        private byte[] readBuffer = new byte[32768];

        private readonly Stream dataStream;

        public DataStreamFileProcedures(Stream data)
        {
            dataStream = data;
        }

        public void Close(IntPtr user)
        {
        }

        public long Length(IntPtr user)
        {
            if (dataStream == null) return 0;

            try
            {
                return dataStream.Length;
            }
            catch
            {
            }

            return 0;
        }

        public int Read(IntPtr buffer, int length, IntPtr user)
        {
            if (dataStream == null) return 0;

            try
            {
                if (length > readBuffer.Length)
                    readBuffer = new byte[length];

                if (!dataStream.CanRead)
                    return 0;

                int readBytes = dataStream.Read(readBuffer, 0, length);
                Marshal.Copy(readBuffer, 0, buffer, readBytes);
                return readBytes;
            }
            catch
            {
            }

            return 0;
        }

        public bool Seek(long offset, IntPtr user)
        {
            if (dataStream == null) return false;

            try
            {
                return dataStream.Seek(offset, SeekOrigin.Begin) == offset;
            }
            catch
            {
            }

            return false;
        }
    }
}
