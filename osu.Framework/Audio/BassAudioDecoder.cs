// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using ManagedBass;
using osu.Framework.Audio.Callbacks;
using SDL2;
using static osu.Framework.Audio.AudioDecoderManager;

namespace osu.Framework.Audio
{
    /// <summary>
    /// This is only for using BASS as a decoder for SDL2 backend!
    /// </summary>
    internal class BassAudioDecoder : AudioDecoder
    {
        private int decodeStream;
        private FileCallbacks? callbacks;
        private SDL2AudioStream? resampler;

        private byte[]? decodeData;
        private byte[]? resampleData;

        public BassAudioDecoder(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
            : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
        {
        }

        internal override void Free()
        {
            if (decodeStream != 0)
            {
                Bass.StreamFree(decodeStream);
                decodeStream = 0;
            }

            resampler?.Dispose();
            callbacks?.Dispose();

            decodeData = null;
            resampleData = null;

            base.Free();
        }

        private static readonly object bass_sync_lock = new object();

        protected override int LoadFromStreamInternal(out byte[] decoded)
        {
            if (Bass.CurrentDevice < 0)
                throw new InvalidOperationException("Initialize a BASS device to decode audio");

            lock (bass_sync_lock)
            {
                if (!Loading)
                {
                    callbacks = new FileCallbacks(new DataStreamFileProcedures(Stream));
                    BassFlags bassFlags = BassFlags.Decode;
                    if (SDL.SDL_AUDIO_ISFLOAT(Format)) bassFlags |= BassFlags.Float;
                    if (IsTrack) bassFlags |= BassFlags.Prescan;
                    decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, bassFlags, callbacks.Callbacks);

                    if (decodeStream == 0)
                        throw new FormatException($"Couldn't create stream: {Bass.LastError}");

                    bool infoAvail = Bass.ChannelGetInfo(decodeStream, out var info);

                    if (infoAvail)
                    {
                        ByteLength = Bass.ChannelGetLength(decodeStream);
                        Length = Bass.ChannelBytes2Seconds(decodeStream, ByteLength) * 1000;
                        Bitrate = (int)Math.Round(Bass.ChannelGetAttribute(decodeStream, ChannelAttribute.Bitrate));

                        ushort srcformat;

                        switch (info.Resolution)
                        {
                            case Resolution.Byte:
                                srcformat = SDL.AUDIO_S8;
                                break;

                            case Resolution.Short:
                                srcformat = SDL.AUDIO_S16;
                                break;

                            case Resolution.Float:
                            default:
                                srcformat = SDL.AUDIO_F32;
                                break;
                        }

                        if (info.Channels != Channels || srcformat != Format || info.Frequency != Rate)
                        {
                            resampler = new SDL2AudioStream(srcformat, (byte)info.Channels, info.Frequency, Format, (byte)Channels, Rate);
                            ByteLength = (long)Math.Ceiling(ByteLength / (double)info.Frequency / SDL.SDL_AUDIO_BITSIZE(srcformat) / info.Channels
                                                            * Rate * SDL.SDL_AUDIO_BITSIZE(Format) * Channels);
                        }
                    }
                    else
                    {
                        if (IsTrack)
                            throw new FormatException($"Couldn't get channel info: {Bass.LastError}");
                    }

                    Loading = true;
                }

                int bufferLen = (int)(IsTrack ? Bass.ChannelSeconds2Bytes(decodeStream, 1) : ByteLength);

                if (bufferLen <= 0)
                    bufferLen = 44100 * 2 * 4 * 1;

                if (decodeData == null || decodeData.Length < bufferLen)
                {
                    decodeData = new byte[bufferLen];
                }

                int got = Bass.ChannelGetData(decodeStream, decodeData, bufferLen);

                if (got == -1)
                {
                    Loading = false;

                    if (Bass.LastError != Errors.Ended)
                        throw new FormatException($"Couldn't decode: {Bass.LastError}");
                }

                if (Bass.StreamGetFilePosition(decodeStream, FileStreamPosition.End) <= Bass.StreamGetFilePosition(decodeStream))
                    Loading = false;

                if (resampler == null)
                {
                    decoded = decodeData;
                    return Math.Max(0, got);
                }
                else
                {
                    if (got > 0)
                        resampler.Put(decodeData, got);

                    if (!Loading)
                        resampler.Flush();

                    int avail = resampler.GetPendingBytes();

                    if (resampleData == null || resampleData.Length < avail)
                        resampleData = new byte[avail];

                    if (avail > 0)
                        resampler.Get(resampleData, avail);

                    decoded = resampleData;
                    return avail;
                }
            }
        }
    }
}
