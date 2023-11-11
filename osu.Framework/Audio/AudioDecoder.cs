// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using osu.Framework.Logging;
using System.Collections.Generic;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Decodes audio from <see cref="Stream"/>, and convert it to appropriate format.
    /// </summary>
    public abstract class AudioDecoder : IDisposable
    {
        public abstract class AudioDecoderData
        {
            internal readonly int Rate;
            internal readonly int Channels;
            internal readonly bool IsTrack;
            internal readonly ushort Format;
            internal readonly Stream Stream;
            internal readonly bool AutoDisposeStream;
            internal readonly PassDataDelegate? Pass;
            internal readonly object? UserData;

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

            protected AudioDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass, object? userData)
            {
                Rate = rate;
                Channels = channels;
                IsTrack = isTrack;
                Format = format;
                Stream = stream;
                AutoDisposeStream = autoDisposeStream;
                Pass = pass;
                UserData = userData;
            }

            public void Stop()
            {
                StopJob = true;
            }

            // Call this in lock
            internal virtual void Dispose()
            {
                if (AutoDisposeStream)
                    Stream.Dispose();
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

        private readonly int rate;
        private readonly int channels;
        private readonly ushort format;

        private Thread? decoderThread;

        /// <summary>
        /// Set up configuration and start a decoding thread.
        /// </summary>
        /// <param name="rate">Resample rate</param>
        /// <param name="channels">Resample channels</param>
        /// <param name="format">Resample SDL audio format</param>
        protected AudioDecoder(int rate, int channels, ushort format)
        {
            this.rate = rate;
            this.channels = channels;
            this.format = format;
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
            if (decoderThread == null)
            {
                decoderThread = new Thread(() => loop(tokenSource.Token))
                {
                    IsBackground = true
                };

                decoderThread.Start();
            }

            AudioDecoderData data = CreateDecoderData(rate, channels, true, format, stream, true, pass, userData);

            lock (jobs)
                jobs.AddFirst(data);

            return data;
        }

        /// <summary>
        /// Decodes audio from stream. It blocks until decoding is done.
        /// </summary>
        /// <param name="stream">Data stream to read.</param>
        /// <returns>Decoded audio</returns>
        public byte[] DecodeAudioInCurrentSpec(Stream stream) => DecodeAudio(rate, channels, format, stream);

        public byte[] DecodeAudio(int freq, int channels, ushort format, Stream stream)
        {
            AudioDecoderData data = CreateDecoderData(freq, channels, false, format, stream);

            LoadFromStream(data, out byte[] decoded);

            if (!data.Loading)
                return decoded;

            // fallback if it couldn't decode at once
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(decoded);

                while (data.Loading)
                {
                    LoadFromStream(data, out decoded);
                    memoryStream.Write(decoded);
                }

                return memoryStream.ToArray();
            }
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
                            AudioDecoderData data = node.Value;

                            if (data.StopJob)
                            {
                                data.Dispose();
                                jobs.Remove(node);
                            }
                            else
                            {
                                LoadFromStream(data, out byte[] decoded);
                                data.Pass?.Invoke(decoded, data.UserData, data, !data.Loading);
                            }

                            if (!data.Loading)
                                jobs.Remove(node);

                            node = next;
                        }
                    }
                }

                if (jobCount <= 0)
                    Thread.Sleep(50);
            }
        }

        public abstract AudioDecoderData CreateDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream = true, PassDataDelegate? pass = null, object? userData = null);

        protected abstract void LoadFromStreamInternal(AudioDecoderData job, out byte[] decoded);

        /// <summary>
        /// Decodes and resamples audio from job.Stream, and pass it to decoded.
        /// </summary>
        /// <param name="job">Decode data</param>
        /// <param name="decoded">Decoded audio</param>
        public void LoadFromStream(AudioDecoderData job, out byte[] decoded)
        {
            try
            {
                LoadFromStreamInternal(job, out decoded);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, level: LogLevel.Important);
                job.Loading = false;
                decoded = Array.Empty<byte>();
            }
            finally
            {
                if (!job.Loading)
                    job.Dispose();
            }
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
                    decoderThread?.Join();
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
