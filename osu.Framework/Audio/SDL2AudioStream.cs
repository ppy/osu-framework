// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using SDL2;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Wrapper for SDL_AudioStream, which is a built-in audio converter.
    /// </summary>
    public class SDL2AudioStream : AudioComponent
    {
        private IntPtr stream = IntPtr.Zero;

        public ushort SrcFormat { get; private set; }
        public byte SrcChannels { get; private set; }
        public int SrcRate { get; private set; }

        public ushort DstFormat { get; private set; }
        public byte DstChannels { get; private set; }
        public int DstRate { get; private set; }

        /// <summary>
        /// Creates a new <see cref="SDL2AudioStream"/>.
        /// </summary>
        /// <param name="srcFormat">Source SDL_AudioFormat</param>
        /// <param name="srcChannels">Source channels</param>
        /// <param name="srcRate">Source sample rate</param>
        /// <param name="dstFormat">Destination SDL_AudioFormat</param>
        /// <param name="dstChannels">Destination Channels</param>
        /// <param name="dstRate">Destination sample rate</param>
        /// <exception cref="FormatException">Thrown if SDL refuses to create a stream.</exception>
        public SDL2AudioStream(ushort srcFormat, byte srcChannels, int srcRate, ushort dstFormat, byte dstChannels, int dstRate)
        {
            SrcFormat = srcFormat;
            SrcChannels = srcChannels;
            SrcRate = srcRate;

            if (!UpdateStream(dstFormat, dstChannels, dstRate))
                throw new FormatException("Failed creating resampling stream");
        }

        /// <summary>
        /// Recreates the stream.
        /// </summary>
        /// <param name="dstFormat">Destination SDL_AudioFormat</param>
        /// <param name="dstChannels">Destination Channels</param>
        /// <param name="dstRate">Destination sample rate</param>
        /// <returns>False if failed</returns>
        public bool UpdateStream(ushort dstFormat, byte dstChannels, int dstRate)
        {
            if (stream != IntPtr.Zero)
                SDL.SDL_FreeAudioStream(stream);

            // SDL3 may support this in a better way
            stream = SDL.SDL_NewAudioStream(SrcFormat, SrcChannels, SrcRate, dstFormat, dstChannels, dstRate);

            if (stream != IntPtr.Zero)
            {
                DstFormat = dstFormat;
                DstChannels = dstChannels;
                DstRate = dstRate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns available samples in bytes.
        /// </summary>
        public int GetPendingBytes()
        {
            return SDL.SDL_AudioStreamAvailable(stream);
        }

        /// <summary>
        /// Put samples in the stream.
        /// </summary>
        /// <param name="data">Data to put</param>
        /// <param name="len">Data length in bytes</param>
        /// <returns>False if failed</returns>
        public unsafe bool Put(byte[] data, int len)
        {
            fixed (byte* p = data)
            {
                IntPtr ptr = new IntPtr(p);
                return SDL.SDL_AudioStreamPut(stream, ptr, len) == 0;
            }
        }

        /// <summary>
        /// Get samples from the stream.
        /// </summary>
        /// <param name="data">An array that stream will put data into</param>
        /// <param name="len">Maximum data length in bytes</param>
        /// <returns>Returned data length in bytes</returns>
        public unsafe int Get(byte[] data, int len)
        {
            fixed (byte* p = data)
            {
                IntPtr ptr = new IntPtr(p);
                return SDL.SDL_AudioStreamGet(stream, ptr, len);
            }
        }

        // it is not available in sdl2-cs, will make a pr in future
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_AudioStreamFlush(IntPtr stream);

        /// <summary>
        /// Flushes the stream.
        /// </summary>
        public void Flush()
        {
            SDL_AudioStreamFlush(stream);
        }

        /// <summary>
        /// Clears the stream.
        /// </summary>
        public void Clear()
        {
            SDL.SDL_AudioStreamClear(stream);
        }

        ~SDL2AudioStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (stream != IntPtr.Zero)
                SDL.SDL_FreeAudioStream(stream);

            base.Dispose(disposing);
        }
    }
}
