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
        public interface ISDL3AudioDataReceiver
        {
            /// <summary>
            /// Interface to get decoded audio data from the decoder.
            /// </summary>
            /// <param name="data">Decoded audio. The format depends on <see cref="SDL3AudioDecoder.AudioSpec"/> you specified,
            /// so you may need <see cref="Buffer.BlockCopy(Array, int, Array, int, int)"/> to actual data format.
            /// This may be used by decoder later to reduce allocation, so you need to copy the data before exiting from this delegate, otherwise you may end up with wrong data.</param>
            /// <param name="length">Length in byte of decoded audio. Use this instead of data.Length</param>
            /// <param name="done">Whether if this is the last data or not.</param>
            void GetData(byte[] data, int length, bool done);

            void GetMetaData(int bitrate, double length, long byteLength);
        }

        private readonly LinkedList<SDL3AudioDecoder> jobs = new LinkedList<SDL3AudioDecoder>();

        private readonly Thread decoderThread;
        private readonly AutoResetEvent decoderWaitHandle;
        private readonly CancellationTokenSource tokenSource;

        /// <summary>
        /// Creates a new decoder that is not managed by the decoder thread.
        /// </summary>
        /// <param name="stream">Refer to <see cref="SDL3AudioDecoder.Stream"/></param>
        /// <param name="audioSpec">Refer to <see cref="SDL3AudioDecoder.AudioSpec"/></param>
        /// <param name="isTrack">Refer to <see cref="SDL3AudioDecoder.IsTrack"/></param>
        /// <param name="autoDisposeStream">Refer to <see cref="SDL3AudioDecoder.AutoDisposeStream"/></param>
        /// <param name="pass">Refer to <see cref="SDL3AudioDecoder.Pass"/></param>
        /// <returns>A new instance.</returns>
        internal static SDL3AudioDecoder CreateDecoder(Stream stream, SDL_AudioSpec audioSpec, bool isTrack, bool autoDisposeStream = true, ISDL3AudioDataReceiver? pass = null)
        {
            SDL3AudioDecoder decoder = Bass.CurrentDevice >= 0
                ? new SDL3AudioDecoder.BassAudioDecoder(stream, audioSpec, isTrack, autoDisposeStream, pass)
                : new SDL3AudioDecoder.FFmpegAudioDecoder(stream, audioSpec, isTrack, autoDisposeStream, pass);

            return decoder;
        }

        private readonly bool bassInit;

        /// <summary>
        /// Starts a decoder thread.
        /// </summary>
        public SDL3AudioDecoderManager()
        {
            tokenSource = new CancellationTokenSource();
            decoderWaitHandle = new AutoResetEvent(false);

            decoderThread = new Thread(() => loop(tokenSource.Token))
            {
                IsBackground = true
            };

            Bass.Configure((ManagedBass.Configuration)68, 1);

            if (Bass.CurrentDevice < 0)
                bassInit = Bass.Init(Bass.NoSoundDevice);

            decoderThread.Start();
        }

        /// <summary>
        /// Creates a new decoder, and adds it to the job list of a decoder thread.
        /// </summary>
        /// <param name="stream">Refer to <see cref="SDL3AudioDecoder.Stream"/></param>
        /// <param name="audioSpec">Refer to <see cref="SDL3AudioDecoder.AudioSpec"/></param>
        /// <param name="isTrack">Refer to <see cref="SDL3AudioDecoder.IsTrack"/></param>
        /// <param name="pass">Refer to <see cref="SDL3AudioDecoder.Pass"/></param>
        /// <returns>A new instance.</returns>
        public SDL3AudioDecoder StartDecodingAsync(Stream stream, SDL_AudioSpec audioSpec, bool isTrack, ISDL3AudioDataReceiver pass)
        {
            if (disposedValue)
                throw new InvalidOperationException($"Cannot start decoding on disposed {nameof(SDL3AudioDecoderManager)}");

            SDL3AudioDecoder decoder = CreateDecoder(stream, audioSpec, isTrack, true, pass);

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
                            SDL3AudioDecoder decoder = node.Value;

                            if (!decoder.StopJob)
                            {
                                try
                                {
                                    int read = decodeAudio(decoder, out byte[] decoded);

                                    if (!decoder.MetadataSended)
                                    {
                                        decoder.MetadataSended = true;
                                        decoder.Pass?.GetMetaData(decoder.Bitrate, decoder.Length, decoder.ByteLength);
                                    }

                                    decoder.Pass?.GetData(decoded, read, !decoder.Loading);
                                }
                                catch (ObjectDisposedException)
                                {
                                    decoder.StopJob = true;
                                }

                                if (!decoder.Loading)
                                    jobs.Remove(node);
                            }
                            else
                            {
                                decoder.Dispose();
                                jobs.Remove(node);
                            }

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
                        job.Dispose();
                    }

                    jobs.Clear();
                }

                if (bassInit)
                {
                    Bass.CurrentDevice = Bass.NoSoundDevice;
                    Bass.Free();
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

        private static int decodeAudio(SDL3AudioDecoder decoder, out byte[] decoded)
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

        /// <summary>
        /// Contains decoder information, and perform the actual decoding.
        /// </summary>
        public abstract class SDL3AudioDecoder
        {
            /// <summary>
            /// Decoder will decode audio data from this.
            /// It accepts most formats. (e.g. MP3, OGG, WAV and so on...)
            /// </summary>
            internal readonly Stream Stream;

            /// <summary>
            /// Decoder will convert audio data according to this spec if needed.
            /// </summary>
            internal readonly SDL_AudioSpec AudioSpec;

            /// <summary>
            /// Decoder will call <see cref="Pass"/> multiple times with partial data if true.
            /// It's a receiver's job to combine the data in this case. Otherwise, It will call only once with the entirely decoded data if false.
            /// </summary>
            internal readonly bool IsTrack;

            /// <summary>
            /// It will automatically dispose <see cref="Stream"/> once decoding is done/failed.
            /// </summary>
            internal readonly bool AutoDisposeStream;

            /// <summary>
            /// Decoder will call <see cref="ISDL3AudioDataReceiver.GetData(byte[], int, bool)"/> once or more to pass the decoded audio data.
            /// </summary>
            internal readonly ISDL3AudioDataReceiver? Pass;

            private int bitrate;

            /// <summary>
            /// Audio bitrate. Decoder may fill this in after the first call of <see cref="LoadFromStream(out byte[])"/>.
            /// </summary>
            public int Bitrate
            {
                get => bitrate;
                set => Interlocked.Exchange(ref bitrate, value);
            }

            private double length;

            /// <summary>
            /// Audio length in milliseconds. Decoder may fill this in after the first call of <see cref="LoadFromStream(out byte[])"/>.
            /// </summary>
            public double Length
            {
                get => length;
                set => Interlocked.Exchange(ref length, value);
            }

            private long byteLength;

            /// <summary>
            /// Audio length in byte. Note that this may not be accurate. You cannot depend on this value entirely.
            /// You can find out the actual byte length by summing up byte counts you received once decoding is done.
            /// Decoder may fill this in after the first call of <see cref="LoadFromStream(out byte[])"/>.
            /// </summary>
            public long ByteLength
            {
                get => byteLength;
                set => Interlocked.Exchange(ref byteLength, value);
            }

            internal bool MetadataSended;

            internal volatile bool StopJob;

            private volatile bool loading;

            /// <summary>
            /// Whether it is decoding or not.
            /// </summary>
            public bool Loading { get => loading; protected set => loading = value; }

            protected SDL3AudioDecoder(Stream stream, SDL_AudioSpec audioSpec, bool isTrack, bool autoDisposeStream, ISDL3AudioDataReceiver? pass)
            {
                Stream = stream;
                AudioSpec = audioSpec;
                IsTrack = isTrack;
                AutoDisposeStream = autoDisposeStream;
                Pass = pass;
            }

            /// <summary>
            /// Add a flag to stop decoding in the next loop of decoder thread.
            /// </summary>
            public void Stop()
            {
                StopJob = true;
            }

            // Not using IDisposable since things must be handled in a decoder thread
            internal virtual void Dispose()
            {
                if (AutoDisposeStream)
                    Stream.Dispose();
            }

            protected abstract int LoadFromStreamInternal(out byte[] decoded);

            /// <summary>
            /// Decodes and resamples audio from job.Stream, and pass it to decoded.
            /// You may need to run this multiple times.
            /// Don't call this yourself if this decoder is in the decoder thread job list.
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
                        Dispose();
                }

                return read;
            }

            /// <summary>
            /// This is only for using BASS as a decoder for SDL3 backend!
            /// </summary>
            internal class BassAudioDecoder : SDL3AudioDecoder
            {
                private int decodeStream;
                private FileCallbacks? fileCallbacks;

                private int resampler;

                private byte[]? decodeData;

                private Resolution resolution
                {
                    get
                    {
                        if (AudioSpec.format == SDL_AudioFormat.SDL_AUDIO_S8)
                            return Resolution.Byte;
                        else if (AudioSpec.format == SDL3.SDL_AUDIO_S16) // uses constant due to endian
                            return Resolution.Short;
                        else
                            return Resolution.Float;
                    }
                }

                private ushort bits => (ushort)SDL3.SDL_AUDIO_BITSIZE(AudioSpec.format);

                public BassAudioDecoder(Stream stream, SDL_AudioSpec audioSpec, bool isTrack, bool autoDisposeStream, ISDL3AudioDataReceiver? pass)
                    : base(stream, audioSpec, isTrack, autoDisposeStream, pass)
                {
                }

                internal override void Dispose()
                {
                    fileCallbacks?.Dispose();
                    fileCallbacks = null;

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

                    base.Dispose();
                }

                protected override int LoadFromStreamInternal(out byte[] decoded)
                {
                    if (Bass.CurrentDevice < 0)
                        throw new InvalidOperationException($"Initialize a BASS device to decode audio: {Bass.LastError}");

                    if (!Loading)
                    {
                        fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(Stream));

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

                            if (info.Channels != AudioSpec.channels || info.Frequency != AudioSpec.freq)
                            {
                                resampler = BassMix.CreateMixerStream(AudioSpec.freq, AudioSpec.channels, BassFlags.MixerEnd | BassFlags.Decode | resolution.ToBassFlag());

                                if (resampler == 0)
                                    throw new FormatException($"Failed to create BASS Mixer: {Bass.LastError}");

                                if (!BassMix.MixerAddChannel(resampler, decodeStream, BassFlags.MixerChanNoRampin | BassFlags.MixerChanLimit))
                                    throw new FormatException($"Failed to add a channel to BASS Mixer: {Bass.LastError}");

                                ByteLength /= info.Channels * (bits / 8);
                                ByteLength = (long)Math.Ceiling((decimal)ByteLength / info.Frequency * AudioSpec.freq);
                                ByteLength *= AudioSpec.channels * (bits / 8);
                            }
                        }
                        else
                        {
                            if (IsTrack)
                                throw new FormatException($"Couldn't get channel info: {Bass.LastError}");
                        }

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
                    else if (got < bufferLen)
                    {
                        // originally used synchandle to detect end, but it somehow created strong handle
                        Loading = false;
                    }

                    decoded = decodeData;
                    return Math.Max(0, got);
                }
            }

            internal class FFmpegAudioDecoder : SDL3AudioDecoder
            {
                private VideoDecoder? ffmpeg;

                public FFmpegAudioDecoder(Stream stream, SDL_AudioSpec audioSpec, bool isTrack, bool autoDisposeStream, ISDL3AudioDataReceiver? pass)
                    : base(stream, audioSpec, isTrack, autoDisposeStream, pass)
                {
                }

                internal override void Dispose()
                {
                    ffmpeg?.Dispose();
                    ffmpeg = null;

                    base.Dispose();
                }

                protected override int LoadFromStreamInternal(out byte[] decoded)
                {
                    if (ffmpeg == null)
                    {
                        ffmpeg = new VideoDecoder(Stream, AudioSpec.freq, AudioSpec.channels,
                            SDL3.SDL_AUDIO_ISFLOAT(AudioSpec.format), SDL3.SDL_AUDIO_BITSIZE(AudioSpec.format), SDL3.SDL_AUDIO_ISSIGNED(AudioSpec.format));

                        ffmpeg.PrepareDecoding();
                        ffmpeg.OpenAudioStream();

                        Bitrate = (int)ffmpeg.AudioBitrate;
                        Length = ffmpeg.Duration;
                        ByteLength = (long)Math.Ceiling(ffmpeg.Duration / 1000.0d * AudioSpec.freq) * AudioSpec.channels * (SDL3.SDL_AUDIO_BITSIZE(AudioSpec.format) / 8); // FIXME

                        Loading = true;
                    }

                    int got = ffmpeg.DecodeNextAudioFrame(32, out decoded, !IsTrack);

                    if (ffmpeg.State != VideoDecoder.DecoderState.Running)
                        Loading = false;

                    return got;
                }
            }
        }
    }
}
