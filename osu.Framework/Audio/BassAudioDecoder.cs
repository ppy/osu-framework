// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using ManagedBass;
using ManagedBass.Mix;
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
        private FileCallbacks? fileCallbacks;

        private SyncCallback? syncCallback;

        private int resampler;

        private byte[]? decodeData;

        private Resolution resolution
        {
            get
            {
                switch (Format)
                {
                    case SDL.AUDIO_S8:
                        return Resolution.Byte;

                    case SDL.AUDIO_S16:
                        return Resolution.Short;

                    case SDL.AUDIO_F32:
                    default:
                        return Resolution.Float;
                }
            }
        }

        private ushort bits => SDL.SDL_AUDIO_BITSIZE(Format);

        public BassAudioDecoder(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
            : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
        {
        }

        internal override void Free()
        {
            if (resampler != 0)
            {
                Bass.StreamFree(resampler);
                resampler = 0;
            }

            if (decodeStream != 0)
            {
                Bass.StreamFree(decodeStream);
                decodeStream = 0;
            }

            fileCallbacks?.Dispose();
            syncCallback?.Dispose();

            fileCallbacks = null;
            syncCallback = null;

            decodeData = null;

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
                    fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(Stream));
                    syncCallback = new SyncCallback((_, _, _, _) =>
                    {
                        Loading = false;
                    });

                    BassFlags bassFlags = BassFlags.Decode | resolution.ToBassFlag();
                    if (IsTrack) bassFlags |= BassFlags.Prescan;

                    decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, bassFlags, fileCallbacks.Callbacks);

                    if (decodeStream == 0)
                        throw new FormatException($"Couldn't create stream: {Bass.LastError}");

                    if (Bass.ChannelGetInfo(decodeStream, out var info))
                    {
                        ByteLength = Bass.ChannelGetLength(decodeStream);
                        Length = Bass.ChannelBytes2Seconds(decodeStream, ByteLength) * 1000.0d;
                        Bitrate = (int)Math.Round(Bass.ChannelGetAttribute(decodeStream, ChannelAttribute.Bitrate));

                        if (info.Channels != Channels || info.Frequency != Rate)
                        {
                            resampler = BassMix.CreateMixerStream(Rate, Channels, BassFlags.MixerEnd | BassFlags.Decode | resolution.ToBassFlag());

                            if (resampler == 0)
                                throw new FormatException($"Failed to create BASS Mixer: {Bass.LastError}");

                            if (!BassMix.MixerAddChannel(resampler, decodeStream, BassFlags.MixerChanNoRampin | BassFlags.MixerChanLimit))
                                throw new FormatException($"Failed to add a channel to BASS Mixer: {Bass.LastError}");

                            ByteLength /= info.Channels * (bits / 8);
                            ByteLength = (long)Math.Ceiling((decimal)ByteLength / info.Frequency * Rate);
                            ByteLength *= Channels * (bits / 8);
                        }
                    }
                    else
                    {
                        if (IsTrack)
                            throw new FormatException($"Couldn't get channel info: {Bass.LastError}");
                    }

                    Bass.ChannelSetSync(resampler == 0 ? decodeStream : resampler, SyncFlags.End | SyncFlags.Onetime, 0, syncCallback.Callback, syncCallback.Handle);

                    Loading = true;
                }

                int handle = resampler == 0 ? decodeStream : resampler;

                int bufferLen = (int)Bass.ChannelSeconds2Bytes(handle, 1);

                if (bufferLen <= 0)
                    bufferLen = 44100 * 2 * 4 * 1;

                if (decodeData == null || decodeData.Length < bufferLen)
                    decodeData = new byte[bufferLen];

                int got = Bass.ChannelGetData(handle, decodeData, bufferLen);

                if (got == -1)
                {
                    Loading = false;

                    if (Bass.LastError != Errors.Ended)
                        throw new FormatException($"Couldn't decode: {Bass.LastError}");
                }

                decoded = decodeData;
                return Math.Max(0, got);
            }
        }
    }
}
