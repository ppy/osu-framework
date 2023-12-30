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
    public class AudioDecoderManager : IDisposable
    {
        public abstract class AudioDecoder
        {
            protected readonly int Rate;
            protected readonly int Channels;
            protected readonly bool IsTrack;
            protected readonly ushort Format;
            protected readonly Stream Stream;
            protected readonly bool AutoDisposeStream;
            protected readonly PassDataDelegate? Pass;

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

            protected AudioDecoder(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
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

            // Not using IDisposable since things must be handled in a specific thread
            internal virtual void Free()
            {
                if (AutoDisposeStream)
                    Stream.Dispose();
            }

            protected abstract int LoadFromStreamInternal(out byte[] decoded);

            /// <summary>
            /// Decodes and resamples audio from job.Stream, and pass it to decoded.
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

                Pass?.Invoke(decoded, read, this, !Loading);
                return read;
            }
        }

        private readonly LinkedList<AudioDecoder> jobs = new LinkedList<AudioDecoder>();

        public delegate void PassDataDelegate(byte[] data, int length, AudioDecoder decoderData, bool done);

        private Thread? decoderThread;
        private AutoResetEvent? decoderWaitHandle;

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        internal static AudioDecoder CreateDecoder(int rate, int channels, bool isTrack, ushort format, Stream stream,
                                                   bool autoDisposeStream = true, PassDataDelegate? pass = null)
        {
            AudioDecoder decoder = ManagedBass.Bass.CurrentDevice >= 0
                ? new BassAudioDecoder(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
                : new FFmpegAudioDecoder(rate, channels, isTrack, format, stream, autoDisposeStream, pass);

            return decoder;
        }

        public AudioDecoder StartDecodingAsync(int rate, byte channels, ushort format, Stream stream, PassDataDelegate pass)
        {
            if (decoderThread == null)
            {
                decoderWaitHandle = new AutoResetEvent(false);

                decoderThread = new Thread(() => loop(tokenSource.Token))
                {
                    IsBackground = true
                };

                decoderThread.Start();
            }

            AudioDecoder decoder = CreateDecoder(rate, channels, true, format, stream, true, pass);

            lock (jobs)
                jobs.AddFirst(decoder);

            decoderWaitHandle?.Set();

            return decoder;
        }

        public static byte[] DecodeAudio(int freq, int channels, ushort format, Stream stream, out int size)
        {
            AudioDecoder decoder = CreateDecoder(freq, channels, false, format, stream);

            int read = decoder.LoadFromStream(out byte[] decoded);

            if (!decoder.Loading)
            {
                size = read;
                return decoded;
            }

            // fallback if it couldn't decode at once
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(decoded, 0, read);

                while (decoder.Loading)
                {
                    read = decoder.LoadFromStream(out decoded);
                    memoryStream.Write(decoded, 0, read);
                }

                size = (int)memoryStream.Length;
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
                            AudioDecoder decoder = node.Value;

                            if (decoder.StopJob)
                            {
                                decoder.Free();
                                jobs.Remove(node);
                            }
                            else
                            {
                                decoder.LoadFromStream(out _);
                            }

                            if (!decoder.Loading)
                                jobs.Remove(node);

                            node = next;
                        }
                    }
                }

                if (jobCount <= 0)
                    decoderWaitHandle?.WaitOne();
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
                    decoderWaitHandle?.Set();

                    tokenSource.Dispose();
                    decoderThread?.Join();
                    decoderWaitHandle?.Dispose();
                }

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

        ~AudioDecoderManager()
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
