// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using osu.Framework.Logging;
using System.Collections.Generic;
using SDL;
using ManagedBass.Mix;
using ManagedBass;
using osu.Framework.Audio.Callbacks;
using osu.Framework.Graphics.Video;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Decodes audio from <see cref="Stream"/>, and convert it to appropriate format.
    /// It needs a lot of polishing...
    /// </summary>
    public class SDL3AudioDecoderManager : IDisposable
    {
        private readonly LinkedList<AudioDecoder> jobs = new LinkedList<AudioDecoder>();

        public delegate void PassDataDelegate(byte[] data, int length, AudioDecoder decoderData, bool done);

        private readonly Thread decoderThread;
        private readonly AutoResetEvent decoderWaitHandle;
        private readonly CancellationTokenSource tokenSource;

        internal static AudioDecoder CreateDecoder(int rate, int channels, bool isTrack, SDL_AudioFormat format, Stream stream,
                                                   bool autoDisposeStream = true, PassDataDelegate? pass = null)
        {
            AudioDecoder decoder = Bass.CurrentDevice >= 0
                ? new BassAudioDecoder(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
                : new FFmpegAudioDecoder(rate, channels, isTrack, format, stream, autoDisposeStream, pass);

            return decoder;
        }

        public SDL3AudioDecoderManager()
        {
            tokenSource = new CancellationTokenSource();
            decoderWaitHandle = new AutoResetEvent(false);

            decoderThread = new Thread(() => loop(tokenSource.Token))
            {
                IsBackground = true
            };

            decoderThread.Start();
        }

        public AudioDecoder StartDecodingAsync(int rate, int channels, SDL_AudioFormat format, Stream stream, PassDataDelegate pass, bool isTrack)
        {
            if (disposedValue)
                throw new InvalidOperationException($"Cannot start decoding on disposed {nameof(SDL3AudioDecoderManager)}");

            AudioDecoder decoder = CreateDecoder(rate, channels, isTrack, format, stream, true, pass);

            lock (jobs)
                jobs.AddFirst(decoder);

            decoderWaitHandle.Set();

            return decoder;
        }

        private void loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                int jobCount;

                lock (jobs)
                {
                    jobCount = jobs.Count;

                    if (jobCount > 0)
                    {
                        var node = jobs.First;

                        while (node != null)
                        {
                            var next = node.Next;
                            AudioDecoder decoder = node.Value;

                            if (decoder.StopJob)
                            {
                                decoder.Free();
                                jobs.Remove(node);
                            }
                            else
                            {
                                int read = decodeAudio(decoder, out byte[] decoded);
                                decoder.Pass?.Invoke(decoded, read, decoder, !decoder.Loading);
                            }

                            if (!decoder.Loading)
                                jobs.Remove(node);

                            node = next;
                        }
                    }
                }

                if (jobCount <= 0)
                    decoderWaitHandle.WaitOne();
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                tokenSource.Cancel();
                decoderWaitHandle.Set();

                decoderThread.Join();
                tokenSource.Dispose();
                decoderWaitHandle.Dispose();

                lock (jobs)
                {
                    foreach (var job in jobs)
                    {
                        job.Free();
                    }

                    jobs.Clear();
                }

                disposedValue = true;
            }
        }

        ~SDL3AudioDecoderManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static int decodeAudio(AudioDecoder decoder, out byte[] decoded)
        {
            int read = decoder.LoadFromStream(out byte[] temp);

            if (!decoder.Loading || decoder.IsTrack)
            {
                decoded = temp;
                return read;
            }

            // fallback if it couldn't decode at once
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(temp, 0, read);

                while (decoder.Loading)
                {
                    read = decoder.LoadFromStream(out temp);
                    memoryStream.Write(temp, 0, read);
                }

                decoded = memoryStream.ToArray();
                return (int)memoryStream.Length;
            }
        }

        public abstract class AudioDecoder
        {
            internal readonly int Rate;
            internal readonly int Channels;
            internal readonly bool IsTrack;
            internal readonly SDL_AudioFormat Format;
            internal readonly Stream Stream;
            internal readonly bool AutoDisposeStream;
            internal readonly PassDataDelegate? Pass;

            private int bitrate;

            public int Bitrate
            {
                get => bitrate;
                set => Interlocked.Exchange(ref bitrate, value);
            }

            private double length;

            public double Length
            {
                get => length;
                set => Interlocked.Exchange(ref length, value);
            }

            private long byteLength;

            public long ByteLength
            {
                get => byteLength;
                set => Interlocked.Exchange(ref byteLength, value);
            }

            internal volatile bool StopJob;

            private volatile bool loading;
            public bool Loading { get => loading; protected set => loading = value; }

            protected AudioDecoder(int rate, int channels, bool isTrack, SDL_AudioFormat format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
            {
                Rate = rate;
                Channels = channels;
                IsTrack = isTrack;
                Format = format;
                Stream = stream;
                AutoDisposeStream = autoDisposeStream;
                Pass = pass;
            }

            public void Stop()
            {
                StopJob = true;
            }

            // Not using IDisposable since things must be handled in a decoder thread
            internal virtual void Free()
            {
                if (AutoDisposeStream)
                    Stream.Dispose();
            }

            protected abstract int LoadFromStreamInternal(out byte[] decoded);

            /// <summary>
            /// Decodes and resamples audio from job.Stream, and pass it to decoded.
            /// You may need to run this multiple times.
            /// </summary>
            /// <param name="decoded">Decoded audio</param>
            public int LoadFromStream(out byte[] decoded)
            {
                int read = 0;

                try
                {
                    read = LoadFromStreamInternal(out decoded);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message, level: LogLevel.Important);
                    Loading = false;
                    decoded = Array.Empty<byte>();
                }
                finally
                {
                    if (!Loading)
                        Free();
                }

                return read;
            }
        }

        /// <summary>
        /// This is only for using BASS as a decoder for SDL3 backend!
        /// </summary>
        internal class BassAudioDecoder : AudioDecoder
        {
            private int decodeStream;
            private FileCallbacks? fileCallbacks;

            private int syncHandle;
            private SyncCallback? syncCallback;

            private int resampler;

            private byte[]? decodeData;

            private Resolution resolution
            {
                get
                {
                    if (Format == SDL_AudioFormat.SDL_AUDIO_S8)
                        return Resolution.Byte;
                    else if (Format == SDL3.SDL_AUDIO_S16) // uses constant due to endian
                        return Resolution.Short;
                    else
                        return Resolution.Float;
                }
            }

            private ushort bits => (ushort)SDL3.SDL_AUDIO_BITSIZE(Format);

            public BassAudioDecoder(int rate, int channels, bool isTrack, SDL_AudioFormat format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
                : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
            {
            }

            internal override void Free()
            {
                if (syncHandle != 0)
                {
                    Bass.ChannelRemoveSync(resampler == 0 ? decodeStream : resampler, syncHandle);
                    syncHandle = 0;
                }

                fileCallbacks?.Dispose();
                syncCallback?.Dispose();

                fileCallbacks = null;
                syncCallback = null;

                decodeData = null;

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

                        syncHandle = Bass.ChannelSetSync(resampler == 0 ? decodeStream : resampler, SyncFlags.End | SyncFlags.Onetime, 0, syncCallback.Callback, syncCallback.Handle);

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

        internal class FFmpegAudioDecoder : AudioDecoder
        {
            private VideoDecoder? ffmpeg;
            private byte[]? decodeData;

            public FFmpegAudioDecoder(int rate, int channels, bool isTrack, SDL_AudioFormat format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
                : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
            {
            }

            internal override void Free()
            {
                decodeData = null;

                ffmpeg?.Dispose();
                base.Free();
            }

            protected override int LoadFromStreamInternal(out byte[] decoded)
            {
                if (ffmpeg == null)
                {
                    ffmpeg = new VideoDecoder(Stream, Rate, Channels, SDL3.SDL_AUDIO_ISFLOAT(Format), SDL3.SDL_AUDIO_BITSIZE(Format), SDL3.SDL_AUDIO_ISSIGNED(Format));

                    ffmpeg.PrepareDecoding();
                    ffmpeg.RecreateCodecContext();

                    Bitrate = (int)ffmpeg.Bitrate;
                    Length = ffmpeg.Duration;
                    ByteLength = (long)Math.Ceiling(ffmpeg.Duration / 1000.0d * Rate) * Channels * (SDL3.SDL_AUDIO_BITSIZE(Format) / 8); // FIXME

                    Loading = true;
                }

                int got = ffmpeg.DecodeNextAudioFrame(32, ref decodeData, !IsTrack);

                if (ffmpeg.State != VideoDecoder.DecoderState.Running)
                    Loading = false;

                decoded = decodeData;
                return got;
            }
        }
    }
}
