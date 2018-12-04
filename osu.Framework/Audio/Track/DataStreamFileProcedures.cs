// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedBass;

namespace osu.Framework.Audio.Track
{
    public class DataStreamFileProcedures
    {
        private byte[] readBuffer = new byte[32768];

        private readonly Stream dataStream;

        public FileProcedures BassProcedures => new FileProcedures
        {
            Close = CloseCallback,
            Length = LengthCallback,
            Read = ReadCallback,
            Seek = SeekCallback
        };

        public DataStreamFileProcedures(Stream data)
        {
            dataStream = data;
        }

        public void CloseCallback(IntPtr user)
        {
            //manually handle closing of stream
        }

        public long LengthCallback(IntPtr user)
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

        public int ReadCallback(IntPtr buffer, int length, IntPtr user)
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

        public bool SeekCallback(long offset, IntPtr user)
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
