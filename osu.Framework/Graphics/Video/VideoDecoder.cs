// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FFmpeg.AutoGen;
using osu.Framework.Graphics.Textures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using SixLabors.ImageSharp.PixelFormats;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using AGffmpeg = FFmpeg.AutoGen.ffmpeg;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a video decoder that can be used convert video streams and files into textures.
    /// </summary>
    public unsafe class VideoDecoder : IDisposable
    {
        /// <summary>
        /// The duration of the video that is being decoded. Can only be queried after the decoder has started decoding has loaded. This value may be an estimate by FFmpeg, depending on the video loaded.
        /// </summary>
        public double Duration => stream == null ? 0 : stream->duration * timeBaseInSeconds * 1000;

        /// <summary>
        /// True if the decoder currently does not decode any more frames, false otherwise.
        /// </summary>
        public bool IsRunning => state == DecoderState.Running;

        /// <summary>
        /// True if the decoder has faulted after starting to decode. You can try to restart a failed decoder by invoking <see cref="StartDecoding"/> again.
        /// </summary>
        public bool IsFaulted => state == DecoderState.Faulted;

        /// <summary>
        /// The timestamp of the last frame that was decoded by this video decoder, or 0 if no frames have been decoded.
        /// </summary>
        public float LastDecodedFrameTime => lastDecodedFrameTime;

        /// <summary>
        /// The frame rate of the video stream this decoder is decoding.
        /// </summary>
        public double FrameRate => stream == null ? 0 : stream->avg_frame_rate.GetValue();

        /// <summary>
        /// True if the decoder can seek, false otherwise. Determined by the stream this decoder was created with.
        /// </summary>
        public bool CanSeek => videoStream.CanSeek;

        /// <summary>
        /// The current state of the <see cref="VideoDecoder"/>, as a bindable.
        /// </summary>
        public IBindable<DecoderState> State => bindableState;

        private readonly Bindable<DecoderState> bindableState = new Bindable<DecoderState>();
        private volatile DecoderState volatileState;

        private DecoderState state
        {
            get => volatileState;
            set
            {
                if (bindableState.Value != value)
                    scheduler?.AddOnce(() => bindableState.Value = value);

                volatileState = value;
            }
        }

        private readonly Scheduler scheduler;

        // libav-context-related
        private AVFormatContext* formatContext;
        private AVStream* stream;
        private AVCodecParameters codecParams;
        private byte* contextBuffer;
        private byte[] managedContextBuffer;

        private avio_alloc_context_read_packet readPacketCallback;
        private avio_alloc_context_seek seekCallback;

        private bool isDisposed;
        private Stream videoStream;

        private double timeBaseInSeconds;

        // frame data
        private AVFrame* frame;
        private AVFrame* ffmpegFrame;
        private IntPtr frameRgbBufferPtr;
        private int uncompressedFrameSize;

        // active decoder state
        private volatile float lastDecodedFrameTime;

        private Task decodingTask;
        private CancellationTokenSource decodingTaskCancellationTokenSource;

        private double? skipOutputUntilTime;

        private readonly ConcurrentQueue<DecodedFrame> decodedFrames;
        private readonly ConcurrentQueue<Action> decoderCommands;

        private readonly ConcurrentQueue<Texture> availableTextures;

        private ObjectHandle<VideoDecoder> handle;

        private readonly FFmpegFuncs ffmpeg;

        internal bool Looping;

        /// <summary>
        /// Creates a new video decoder that decodes the given video file.
        /// </summary>
        /// <param name="filename">The path to the file that should be decoded.</param>
        /// <param name="scheduler">The <see cref="Scheduler"/> to use when scheduling tasks from the decoder thread.</param>
        public VideoDecoder(string filename, Scheduler scheduler)
            : this(File.OpenRead(filename), scheduler)
        {
        }

        /// <summary>
        /// Creates a new video decoder that decodes the given video stream.
        /// </summary>
        /// <param name="videoStream">The stream that should be decoded.</param>
        /// <param name="scheduler">The <see cref="Scheduler"/> to use when scheduling tasks from the decoder thread.</param>
        public VideoDecoder(Stream videoStream, Scheduler scheduler)
        {
            ffmpeg = CreateFuncs();

            this.scheduler = scheduler;
            this.videoStream = videoStream;
            if (!videoStream.CanRead)
                throw new InvalidOperationException($"The given stream does not support reading. A stream used for a {nameof(VideoDecoder)} must support reading.");

            state = DecoderState.Ready;
            decodedFrames = new ConcurrentQueue<DecodedFrame>();
            decoderCommands = new ConcurrentQueue<Action>();
            availableTextures = new ConcurrentQueue<Texture>();
            handle = new ObjectHandle<VideoDecoder>(this, GCHandleType.Normal);
        }

        /// <summary>
        /// Seek the decoder to the given timestamp. This will fail if <see cref="CanSeek"/> is false.
        /// </summary>
        /// <param name="targetTimestamp">The timestamp to seek to.</param>
        public void Seek(double targetTimestamp)
        {
            if (!CanSeek)
                throw new InvalidOperationException("This decoder cannot seek because the underlying stream used to decode the video does not support seeking.");

            decoderCommands.Enqueue(() =>
            {
                ffmpeg.av_seek_frame(formatContext, stream->index, (long)(targetTimestamp / timeBaseInSeconds / 1000.0), AGffmpeg.AVSEEK_FLAG_BACKWARD);
                skipOutputUntilTime = targetTimestamp;
            });
        }

        /// <summary>
        /// Returns the given frames back to the decoder, allowing the decoder to reuse the textures contained in the frames to draw new frames.
        /// </summary>
        /// <param name="frames">The frames that should be returned to the decoder.</param>
        public void ReturnFrames(IEnumerable<DecodedFrame> frames)
        {
            foreach (var f in frames)
            {
                ((TextureGLSingle)f.Texture.TextureGL).FlushUploads();
                availableTextures.Enqueue(f.Texture);
            }
        }

        /// <summary>
        /// Starts the decoding process. The decoding will happen asynchronously in a separate thread. The decoded frames can be retrieved by using <see cref="GetDecodedFrames"/>.
        /// </summary>
        public void StartDecoding()
        {
            // only prepare for decoding if this is our first time starting the decoding process
            if (formatContext == null)
            {
                try
                {
                    prepareDecoding();
                }
                catch (Exception e)
                {
                    Logger.Log($"VideoDecoder faulted: {e}");
                    state = DecoderState.Faulted;
                    return;
                }
            }

            decodingTaskCancellationTokenSource = new CancellationTokenSource();
            decodingTask = Task.Factory.StartNew(() => decodingLoop(decodingTaskCancellationTokenSource.Token), decodingTaskCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Stops the decoding process. Optionally waits for the decoder thread to terminate.
        /// </summary>
        /// <param name="waitForDecoderExit">True if this method should wait for the decoder thread to terminate, false otherwise.</param>
        public void StopDecoding(bool waitForDecoderExit)
        {
            if (decodingTask == null)
                return;

            decodingTaskCancellationTokenSource.Cancel();
            if (waitForDecoderExit)
                decodingTask.Wait();

            decodingTask = null;
            decodingTaskCancellationTokenSource.Dispose();
            decodingTaskCancellationTokenSource = null;

            state = DecoderState.Ready;
        }

        /// <summary>
        /// Gets all frames that have been decoded by the decoder up until the point in time when this method was called.
        /// Retrieving decoded frames using this method consumes them, ie calling this method again will never retrieve the same frame twice.
        /// </summary>
        /// <returns>The frames that have been decoded up until the point in time this method was called.</returns>
        public IEnumerable<DecodedFrame> GetDecodedFrames()
        {
            var frames = new List<DecodedFrame>(decodedFrames.Count);
            while (decodedFrames.TryDequeue(out var df))
                frames.Add(df);

            return frames;
        }

        [MonoPInvokeCallback(typeof(avio_alloc_context_read_packet))]
        private static int readPacket(void* opaque, byte* bufferPtr, int bufferSize)
        {
            var handle = new ObjectHandle<VideoDecoder>((IntPtr)opaque);
            if (!handle.GetTarget(out VideoDecoder decoder))
                return 0;

            if (bufferSize != decoder.managedContextBuffer.Length)
                decoder.managedContextBuffer = new byte[bufferSize];

            var bytesRead = decoder.videoStream.Read(decoder.managedContextBuffer, 0, bufferSize);
            Marshal.Copy(decoder.managedContextBuffer, 0, (IntPtr)bufferPtr, bytesRead);
            return bytesRead;
        }

        [MonoPInvokeCallback(typeof(avio_alloc_context_seek))]
        private static long seek(void* opaque, long offset, int whence)
        {
            var handle = new ObjectHandle<VideoDecoder>((IntPtr)opaque);
            if (!handle.GetTarget(out VideoDecoder decoder))
                return -1;

            if (!decoder.videoStream.CanSeek)
                throw new InvalidOperationException("Tried seeking on a video sourced by a non-seekable stream.");

            switch (whence)
            {
                case StdIo.SEEK_CUR:
                    decoder.videoStream.Seek(offset, SeekOrigin.Current);
                    break;

                case StdIo.SEEK_END:
                    decoder.videoStream.Seek(offset, SeekOrigin.End);
                    break;

                case StdIo.SEEK_SET:
                    decoder.videoStream.Seek(offset, SeekOrigin.Begin);
                    break;

                case AGffmpeg.AVSEEK_SIZE:
                    return decoder.videoStream.Length;

                default:
                    return -1;
            }

            return decoder.videoStream.Position;
        }

        // sets up libavformat state: creates the AVFormatContext, the frames, etc. to start decoding, but does not actually start the decodingLoop
        private void prepareDecoding()
        {
            const int context_buffer_size = 4096;

            // the first call to FFmpeg will throw an exception if the libraries cannot be found
            // this will be safely handled in StartDecoding()
            var fcPtr = ffmpeg.avformat_alloc_context();
            formatContext = fcPtr;
            contextBuffer = (byte*)ffmpeg.av_malloc(context_buffer_size);
            managedContextBuffer = new byte[context_buffer_size];
            readPacketCallback = readPacket;
            seekCallback = seek;
            formatContext->pb = ffmpeg.avio_alloc_context(contextBuffer, context_buffer_size, 0, (void*)handle.Handle, readPacketCallback, null, seekCallback);

            int openInputResult = ffmpeg.avformat_open_input(&fcPtr, "dummy", null, null);
            if (openInputResult < 0)
                throw new Exception($"Error {openInputResult} opening file or stream.");

            int findStreamInfoResult = ffmpeg.avformat_find_stream_info(formatContext, null);
            if (findStreamInfoResult < 0)
                throw new Exception($"Error {findStreamInfoResult} finding stream info.");

            var nStreams = formatContext->nb_streams;

            for (var i = 0; i < nStreams; ++i)
            {
                stream = formatContext->streams[i];

                codecParams = *stream->codecpar;

                if (codecParams.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    timeBaseInSeconds = stream->time_base.GetValue();
                    var codecPtr = ffmpeg.avcodec_find_decoder(codecParams.codec_id);
                    if (codecPtr == null)
                        throw new Exception($"Couldn't find codec with id: {codecParams.codec_id}");

                    int openCodecResult = ffmpeg.avcodec_open2(stream->codec, codecPtr, null);
                    if (openCodecResult < 0)
                        throw new Exception($"Error {openCodecResult} trying to open codec with id: {codecParams.codec_id}");

                    frame = ffmpeg.av_frame_alloc();
                    ffmpegFrame = ffmpeg.av_frame_alloc();

                    uncompressedFrameSize = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height, 1);
                    frameRgbBufferPtr = Marshal.AllocHGlobal(uncompressedFrameSize);

                    var dataArr4 = *(byte_ptrArray4*)&ffmpegFrame->data;
                    var linesizeArr4 = *(int_array4*)&ffmpegFrame->linesize;
                    var result = ffmpeg.av_image_fill_arrays(ref dataArr4, ref linesizeArr4, (byte*)frameRgbBufferPtr, AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height, 1);
                    if (result < 0)
                        throw new Exception("Couldn't fill image arrays.");

                    for (uint j = 0; j < byte_ptrArray4.Size; ++j)
                    {
                        ffmpegFrame->data[j] = dataArr4[j];
                        ffmpegFrame->linesize[j] = linesizeArr4[j];
                    }

                    break;
                }
            }
        }

        private void decodingLoop(CancellationToken cancellationToken)
        {
            var packet = ffmpeg.av_packet_alloc();

            const int max_pending_frames = 3;

            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (decodedFrames.Count < max_pending_frames)
                    {
                        int readFrameResult = ffmpeg.av_read_frame(formatContext, packet);

                        if (readFrameResult >= 0)
                        {
                            state = DecoderState.Running;

                            if (packet->stream_index == stream->index)
                            {
                                int sendPacketResult = ffmpeg.avcodec_send_packet(stream->codec, packet);
                                if (sendPacketResult < 0)
                                    throw new Exception($"Error {sendPacketResult} sending packet.");

                                var result = ffmpeg.avcodec_receive_frame(stream->codec, frame);

                                if (result == 0)
                                {
                                    var frameTime = (frame->best_effort_timestamp - stream->start_time) * timeBaseInSeconds * 1000;

                                    if (!skipOutputUntilTime.HasValue || skipOutputUntilTime.Value < frameTime)
                                    {
                                        skipOutputUntilTime = null;

                                        SwsContext* swsCtx = null;

                                        try
                                        {
                                            swsCtx = ffmpeg.sws_getContext(codecParams.width, codecParams.height, (AVPixelFormat)frame->format, codecParams.width, codecParams.height, AVPixelFormat.AV_PIX_FMT_RGBA, 0, null, null, null);
                                            ffmpeg.sws_scale(swsCtx, frame->data, frame->linesize, 0, frame->height, ffmpegFrame->data, ffmpegFrame->linesize);
                                        }
                                        finally
                                        {
                                            ffmpeg.sws_freeContext(swsCtx);
                                        }

                                        if (!availableTextures.TryDequeue(out var tex))
                                            tex = new Texture(codecParams.width, codecParams.height, true);

                                        var upload = new ArrayPoolTextureUpload(tex.Width, tex.Height);

                                        // todo: can likely make this more efficient
                                        new Span<Rgba32>(ffmpegFrame->data[0], uncompressedFrameSize / 4).CopyTo(upload.RawData);

                                        tex.SetData(upload);
                                        decodedFrames.Enqueue(new DecodedFrame { Time = frameTime, Texture = tex });
                                    }

                                    lastDecodedFrameTime = (float)frameTime;
                                }
                            }
                        }
                        else if (readFrameResult == AGffmpeg.AVERROR_EOF)
                        {
                            if (Looping)
                                Seek(0);
                            else
                                state = DecoderState.EndOfStream;
                        }
                        else
                        {
                            state = DecoderState.Ready;
                            Thread.Sleep(1);
                        }
                    }
                    else
                    {
                        // wait until existing buffers are consumed.
                        state = DecoderState.Ready;
                        Thread.Sleep(1);
                    }

                    while (!decoderCommands.IsEmpty)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        if (decoderCommands.TryDequeue(out var cmd))
                            cmd();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"VideoDecoder faulted: {e}");
                state = DecoderState.Faulted;
            }
            finally
            {
                ffmpeg.av_packet_free(&packet);

                if (state != DecoderState.Faulted)
                    state = DecoderState.Stopped;
            }
        }

        protected virtual FFmpegFuncs CreateFuncs() => new FFmpegFuncs
        {
            av_frame_alloc = AGffmpeg.av_frame_alloc,
            av_frame_free = AGffmpeg.av_frame_free,
            av_image_fill_arrays = AGffmpeg.av_image_fill_arrays,
            av_image_get_buffer_size = AGffmpeg.av_image_get_buffer_size,
            av_malloc = AGffmpeg.av_malloc,
            av_packet_alloc = AGffmpeg.av_packet_alloc,
            av_packet_free = AGffmpeg.av_packet_free,
            av_read_frame = AGffmpeg.av_read_frame,
            av_seek_frame = AGffmpeg.av_seek_frame,
            avcodec_find_decoder = AGffmpeg.avcodec_find_decoder,
            avcodec_open2 = AGffmpeg.avcodec_open2,
            avcodec_receive_frame = AGffmpeg.avcodec_receive_frame,
            avcodec_send_packet = AGffmpeg.avcodec_send_packet,
            avformat_alloc_context = AGffmpeg.avformat_alloc_context,
            avformat_close_input = AGffmpeg.avformat_close_input,
            avformat_find_stream_info = AGffmpeg.avformat_find_stream_info,
            avformat_open_input = AGffmpeg.avformat_open_input,
            avio_alloc_context = AGffmpeg.avio_alloc_context,
            sws_freeContext = AGffmpeg.sws_freeContext,
            sws_getContext = AGffmpeg.sws_getContext,
            sws_scale = AGffmpeg.sws_scale
        };

        #region Disposal

        ~VideoDecoder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            videoStream.Dispose();
            videoStream = null;

            while (decoderCommands.TryDequeue(out var _))
            {
            }

            StopDecoding(true);

            if (formatContext != null)
            {
                fixed (AVFormatContext** ptr = &formatContext)
                    ffmpeg.avformat_close_input(ptr);
            }

            seekCallback = null;
            readPacketCallback = null;
            managedContextBuffer = null;

            // gets freed by libavformat when closing the input
            contextBuffer = null;

            if (frame != null)
            {
                fixed (AVFrame** ptr = &frame)
                    ffmpeg.av_frame_free(ptr);
            }

            if (ffmpegFrame != null)
            {
                fixed (AVFrame** ptr = &ffmpegFrame)
                    ffmpeg.av_frame_free(ptr);
            }

            if (frameRgbBufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(frameRgbBufferPtr);
                frameRgbBufferPtr = IntPtr.Zero;
            }

            while (decodedFrames.TryDequeue(out var f))
                f.Texture.Dispose();

            handle.Dispose();
        }

        #endregion

        /// <summary>
        /// Represents the possible states the decoder can be in.
        /// </summary>
        public enum DecoderState
        {
            /// <summary>
            /// The decoder is ready to begin decoding. This is the default state before the decoder starts operations.
            /// </summary>
            Ready = 0,

            /// <summary>
            /// The decoder is currently running and decoding frames.
            /// </summary>
            Running = 1,

            /// <summary>
            /// The decoder has faulted with an exception.
            /// </summary>
            Faulted = 2,

            /// <summary>
            /// The decoder has reached the end of the video data.
            /// </summary>
            EndOfStream = 3,

            /// <summary>
            /// The decoder has been completely stopped and cannot be resumed.
            /// </summary>
            Stopped = 4,
        }
    }
}
