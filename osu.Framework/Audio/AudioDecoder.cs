// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using ManagedBass;
using osu.Framework.Audio.Callbacks;
using SDL2;
using System.Threading;
using osu.Framework.Logging;
using System.Collections.Generic;
using osu.Framework.Graphics.Video;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Decodes audio from <see cref="Stream"/>, and convert it to appropriate format.
    /// </summary>
    public class AudioDecoder : IDisposable
    {
        public class AudioDecoderData
        {
            internal readonly int Rate;
            internal readonly int Channels;
            internal readonly bool IsTrack;
            internal readonly ushort Format;
            internal readonly Stream Stream;
            internal readonly PassDataDelegate Pass;
            internal readonly object? UserData;

            internal int DecodeStream;
            internal FileCallbacks? Callbacks;
            internal SDL2AudioStream? Resampler;

            internal VideoDecoder? FFmpeg;

            private volatile int bitrate;

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

            private long bytelength;

            public long ByteLength
            {
                get => Interlocked.Read(ref bytelength);
                set => Interlocked.Exchange(ref bytelength, value);
            }

            internal volatile bool StopJob;
            internal volatile bool Loading;

            internal AudioDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, PassDataDelegate pass, object? userData)
            {
                Rate = rate;
                Channels = channels;
                IsTrack = isTrack;
                Format = format;
                Stream = stream;
                Pass = pass;
                UserData = userData;
            }

            public void Stop()
            {
                StopJob = true;
            }

            // Call this in lock
            internal void Dispose()
            {
                if (DecodeStream != 0)
                {
                    Bass.StreamFree(DecodeStream);
                    DecodeStream = 0;
                }

                Stream.Dispose();
                Resampler?.Dispose();
                Callbacks?.Dispose();
                FFmpeg?.Dispose();
            }
        }

        private readonly LinkedList<AudioDecoderData> jobs = new LinkedList<AudioDecoderData>();

        /// <summary>
        /// Decoder will call this delegate every time some amount of data is ready.
        /// </summary>
        /// <param name="data">Decoded audio data</param>
        /// <param name="userdata"></param>
        /// <param name="decoderData"></param>
        public delegate void PassDataDelegate(byte[] data, object? userdata, AudioDecoderData decoderData, bool done);

        private readonly SDL.SDL_AudioSpec spec;

        private readonly Thread decoderThread;

        /// <summary>
        /// Set up configuration and start a decoding thread.
        /// </summary>
        /// <param name="spec">Resample format</param>
        public AudioDecoder(SDL.SDL_AudioSpec spec)
        {
            this.spec = spec;

            decoderThread = new Thread(() => loop(tokenSource.Token))
            {
                IsBackground = true
            };

            decoderThread.Start();
        }

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Start decoding in the decoding thread.
        /// </summary>
        /// <param name="stream">Data stream to read</param>
        /// <param name="pass">Delegate to pass data to</param>
        /// <param name="userData">Object to pass to the delegate</param>
        /// <returns></returns>
        public AudioDecoderData StartDecodingAsync(Stream stream, PassDataDelegate pass, object? userData)
        {
            AudioDecoderData data = new AudioDecoderData(spec.freq, spec.channels, true, spec.format, stream, pass, userData);

            lock (jobs)
            {
                jobs.AddFirst(data);
            }

            return data;
        }

        private void passAudioSync(byte[] data, object? userdata, AudioDecoderData decoderData, bool done)
        {
            if (userdata is TempDecodeData temp)
            {
                if (done && temp.Stream == null)
                    temp.DecodedAtOnce = data;
                else
                {
                    temp.Stream ??= new MemoryStream();
                    temp.Stream.Write(data);
                }
            }
        }

        private class TempDecodeData
        {
            internal MemoryStream? Stream;
            internal byte[]? DecodedAtOnce;
        }

        /// <summary>
        /// Decodes audio from stream. It blocks until decoding is done.
        /// </summary>
        /// <param name="stream">Data stream to read.</param>
        /// <returns>Decoded audio</returns>
        public byte[] DecodeAudio(Stream stream)
        {
            TempDecodeData tempData = new TempDecodeData();
            AudioDecoderData data = new AudioDecoderData(spec.freq, spec.channels, false, spec.format, stream, passAudioSync, tempData);

            try
            {
                loadFromStream(data);

                if (tempData.DecodedAtOnce != null)
                    return tempData.DecodedAtOnce;

                while (data.Loading)
                {
                    loadFromStream(data);
                }

                return tempData.Stream?.ToArray() ?? Array.Empty<byte>();
            }
            finally
            {
                tempData.Stream?.Dispose();
                tempData.Stream = null;
            }
        }

        private void loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (jobs.Count == 0)
                {
                    Thread.Sleep(50);
                    continue;
                }

                lock (jobs)
                {
                    var node = jobs.First;

                    while (node != null)
                    {
                        var next = node.Next;
                        AudioDecoderData data = node.Value;

                        if (data.StopJob)
                        {
                            data.Dispose();
                            jobs.Remove(node);
                        }
                        else
                            loadFromStream(data);

                        if (!data.Loading)
                            jobs.Remove(node);

                        node = next;
                    }
                }
            }
        }

        private int loadFromStream(AudioDecoderData job)
        {
            try
            {
                if (Bass.CurrentDevice > -1)
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

                    if (job.Loading)
                    {
                        if (job.DecodeStream == 0)
                            throw new InvalidOperationException("BASS stream is not available");

                        int bufferLen = (int)(job.IsTrack ? Bass.ChannelSeconds2Bytes(job.DecodeStream, 8) : job.ByteLength);

                        if (bufferLen <= 0)
                            bufferLen = 44100 * 2 * 4;

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

                            job.Pass(buffer, job.UserData, job, !job.Loading);
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

                            job.Pass(resampled, job.UserData, job, !job.Loading);
                        }
                    }
                }
                else
                {
                    if (job.FFmpeg == null)
                    {
                        job.FFmpeg = new VideoDecoder(job.Stream, job.Rate, job.Channels, SDL.SDL_AUDIO_ISFLOAT(job.Format), SDL.SDL_AUDIO_BITSIZE(job.Format), SDL.SDL_AUDIO_ISSIGNED(job.Format));

                        job.FFmpeg.PrepareDecoding();
                        job.FFmpeg.RecreateCodecContext();

                        job.Bitrate = (int)job.FFmpeg.Bitrate;
                        job.Length = job.FFmpeg.Duration;
                        job.ByteLength = (long)Math.Ceiling(job.FFmpeg.Duration / 1000.0d * job.Rate) * job.Channels * SDL.SDL_AUDIO_BITSIZE(job.Format); // FIXME

                        job.Loading = true;
                    }

                    job.FFmpeg.DecodeNextAudioFrame(32, out byte[] decoded, !job.IsTrack);

                    if (job.FFmpeg.State != VideoDecoder.DecoderState.Running)
                        job.Loading = false;

                    job.Pass(decoded, job.UserData, job, !job.Loading);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, level: LogLevel.Important);
                job.Loading = false;
            }
            finally
            {
                if (!job.Loading)
                    job.Dispose();
            }

            return 0;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    decoderThread.Join();
                }

                lock (jobs)
                {
                    foreach (var job in jobs)
                    {
                        job.Dispose();
                    }

                    jobs.Clear();
                }

                disposedValue = true;
            }
        }

        ~AudioDecoder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
