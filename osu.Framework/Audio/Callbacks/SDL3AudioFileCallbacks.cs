// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mysoundlib_cs;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Video;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Callbacks
{
    public unsafe class SDL3AudioFileCallbacks : BassCallback
    {
        protected override bool CreateHandle => true;

        private static readonly MySoundLibrary.AvioAllocContextReadPacketFunc read_func = new MySoundLibrary.AvioAllocContextReadPacketFunc
        {
            FuncPtr = &readCallback
        };

        private static readonly MySoundLibrary.AvioAllocContextSeekFunc seek_func = new MySoundLibrary.AvioAllocContextSeekFunc
        {
            FuncPtr = &seekCallback
        };

        public MySoundLibrary.AvioAllocContextReadPacketFunc ReadCallback => read_func;

        public MySoundLibrary.AvioAllocContextSeekFunc SeekCallback => seek_func;

        private readonly Stream stream;

        public SDL3AudioFileCallbacks(Stream stream)
        {
            this.stream = stream;
        }

        private int read(void* opaque, byte* bufferPtr, int bufferSize)
        {
            try
            {
                var span = new Span<byte>(bufferPtr, bufferSize);
                int bytesRead = stream.Read(span);
                return bytesRead != 0 ? bytesRead : FFmpegFuncs.AVERROR_EOF;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Audio read error");
                return 0;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int readCallback(void* opaque, byte* buf, int bufSize)
        {
            var ptr = new ObjectHandle<SDL3AudioFileCallbacks>((IntPtr)opaque);
            if (ptr.GetTarget(out SDL3AudioFileCallbacks target))
                return target.read(opaque, buf, bufSize);

            return FFmpegFuncs.AVERROR_EOF;
        }

        private long seek(void* opaque, long offset, int whence)
        {
            try
            {
                switch (whence)
                {
                    case StdIo.SEEK_CUR:
                        stream.Seek(offset, SeekOrigin.Current);
                        break;

                    case StdIo.SEEK_END:
                        stream.Seek(offset, SeekOrigin.End);
                        break;

                    case StdIo.SEEK_SET:
                        stream.Seek(offset, SeekOrigin.Begin);
                        break;

                    case FFmpegFuncs.AVSEEK_SIZE:
                        return stream.Length;

                    default:
                        return -1;
                }
            }
            catch (Exception e)
            {
                Logger.Log("Audio seek error: " + e.Message);
                return -1;
            }

            return stream.Position;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long seekCallback(void* opaque, long offset, int whence)
        {
            var ptr = new ObjectHandle<SDL3AudioFileCallbacks>((IntPtr)opaque);
            if (ptr.GetTarget(out SDL3AudioFileCallbacks target))
                return target.seek(opaque, offset, whence);

            return -1;
        }
    }
}
