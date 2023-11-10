// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using ManagedBass;
using osu.Framework.Audio.Callbacks;
using SDL2;

namespace osu.Framework.Audio
{
    /// <summary>
    /// This is only for using BASS as a decoder for SDL2 backend!
    /// </summary>
    internal class BassAudioDecoder : AudioDecoder
    {
        public BassAudioDecoder(int rate, int channels, ushort format)
            : base(rate, channels, format)
        {
        }

        public class BassAudioDecoderData : AudioDecoderData
        {
            internal int DecodeStream;
            internal FileCallbacks? Callbacks;
            internal SDL2AudioStream? Resampler;

            public BassAudioDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, PassDataDelegate? pass = null, object? userData = null)
                : base(rate, channels, isTrack, format, stream, pass, userData)
            {
            }

            internal override void Dispose()
            {
                if (DecodeStream != 0)
                {
                    Bass.StreamFree(DecodeStream);
                    DecodeStream = 0;
                }

                Resampler?.Dispose();
                Callbacks?.Dispose();

                base.Dispose();
            }
        }

        private static readonly object bass_sync_lock = new object();

        protected override void LoadFromStreamInternal(AudioDecoderData decodeData, out byte[] decoded)
        {
            if (decodeData is not BassAudioDecoderData job)
                throw new ArgumentException("Provide proper data");

            if (Bass.CurrentDevice < 0)
                throw new InvalidOperationException("Initialize a BASS device to decode audio");

            lock (bass_sync_lock)
            {
                if (!job.Loading)
                {
                    job.Callbacks = new FileCallbacks(new DataStreamFileProcedures(job.Stream));
                    BassFlags bassFlags = BassFlags.Decode;
                    if (SDL.SDL_AUDIO_ISFLOAT(job.Format)) bassFlags |= BassFlags.Float;
                    if (job.IsTrack) bassFlags |= BassFlags.Prescan;
                    job.DecodeStream = Bass.CreateStream(StreamSystem.NoBuffer, bassFlags, job.Callbacks.Callbacks);

                    if (job.DecodeStream == 0)
                        throw new FormatException($"Couldn't create stream: {Bass.LastError}");

                    bool infoAvail = Bass.ChannelGetInfo(job.DecodeStream, out var info);

                    if (infoAvail)
                    {
                        job.ByteLength = Bass.ChannelGetLength(job.DecodeStream);
                        job.Length = Bass.ChannelBytes2Seconds(job.DecodeStream, job.ByteLength) * 1000;
                        job.Bitrate = (int)Math.Round(Bass.ChannelGetAttribute(job.DecodeStream, ChannelAttribute.Bitrate));

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

                        if (info.Channels != job.Channels || srcformat != job.Format || info.Frequency != job.Rate)
                            job.Resampler = new SDL2AudioStream(srcformat, (byte)info.Channels, info.Frequency, job.Format, (byte)job.Channels, job.Rate);
                    }
                    else
                    {
                        if (job.IsTrack)
                            throw new FormatException($"Couldn't get channel info: {Bass.LastError}");
                    }

                    job.Loading = true;
                }

                int bufferLen = (int)(job.IsTrack ? Bass.ChannelSeconds2Bytes(job.DecodeStream, 1) : job.ByteLength);

                if (bufferLen <= 0)
                    bufferLen = 44100 * 2 * 1;

                byte[] buffer = new byte[bufferLen];
                int got = Bass.ChannelGetData(job.DecodeStream, buffer, bufferLen);

                if (got == -1)
                {
                    job.Loading = false;

                    if (Bass.LastError != Errors.Ended)
                        throw new FormatException($"Couldn't decode: {Bass.LastError}");
                }

                if (Bass.StreamGetFilePosition(job.DecodeStream, FileStreamPosition.End) <= Bass.StreamGetFilePosition(job.DecodeStream))
                    job.Loading = false;

                if (job.Resampler == null)
                {
                    if (got <= 0) buffer = Array.Empty<byte>();
                    else if (got != bufferLen) Array.Resize(ref buffer, got);

                    decoded = buffer;
                }
                else
                {
                    if (got > 0)
                        job.Resampler.Put(buffer, got);

                    if (!job.Loading)
                        job.Resampler.Flush();

                    int avail = job.Resampler.GetPendingBytes();

                    byte[] resampled = avail > 0 ? new byte[avail] : Array.Empty<byte>();

                    if (avail > 0)
                        job.Resampler.Get(resampled, avail);

                    decoded = resampled;
                }
            }
        }

        public override AudioDecoderData CreateDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, PassDataDelegate? pass = null, object? userData = null)
            => new BassAudioDecoderData(rate, channels, isTrack, format, stream, pass, userData);
    }
}
