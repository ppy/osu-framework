﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Textures;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a video decoder that can be used convert video streams and files into textures.
    /// </summary>
    public unsafe class FfmpegVideoDecoder : VideoDecoder
    {
        /// <summary>
        /// The duration of the video that is being decoded. Can only be queried after the decoder has started decoding has loaded. This value may be an estimate by FFmpeg, depending on the video loaded.
        /// </summary>
        public override double Duration => stream->duration * timeBaseInSeconds * 1000;

        /// <summary>
        /// The timestamp of the last frame that was decoded by this video decoder, or 0 if no frames have been decoded.
        /// </summary>
        public override float LastDecodedFrameTime => lastDecodedFrameTime;

        /// <summary>
        /// The frame rate of the video stream this decoder is decoding.
        /// </summary>
        public override double FrameRate => stream->avg_frame_rate.GetValue();

        /// <summary>
        /// The current decoding state.
        /// </summary>
        public override DecoderState State
        {
            get => state;
            protected set => state = value;
        }

        private volatile DecoderState state;

        // libav-context-related
        private AVFormatContext* formatContext;
        private AVStream* stream;
        private AVCodecParameters codecParams;
        private byte* contextBuffer;
        private byte[] managedContextBuffer;

        private avio_alloc_context_read_packet readPacketCallback;
        private avio_alloc_context_seek seekCallback;

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

        private readonly ConcurrentQueue<Action> decoderCommands = new ConcurrentQueue<Action>();

        /// <summary>
        /// Creates a new video decoder that decodes the given video file.
        /// </summary>
        /// <param name="filename">The path to the file that should be decoded.</param>
        public FfmpegVideoDecoder(string filename)
            : this(File.OpenRead(filename))
        {
        }

        /// <summary>
        /// Creates a new video decoder that decodes the given video stream.
        /// </summary>
        /// <param name="videoStream">The stream that should be decoded.</param>
        public FfmpegVideoDecoder(Stream videoStream)
            : base(videoStream)
        {
        }

        /// <summary>
        /// Seek the decoder to the given timestamp. This will fail if <see cref="VideoDecoder.CanSeek"/> is false.
        /// </summary>
        /// <param name="targetTimestamp">The timestamp to seek to.</param>
        public override void Seek(double targetTimestamp)
        {
            if (!CanSeek)
                throw new InvalidOperationException("This decoder cannot seek because the underlying stream used to decode the video does not support seeking.");

            decoderCommands.Enqueue(() =>
            {
                ffmpeg.av_seek_frame(formatContext, stream->index, (long)(targetTimestamp / timeBaseInSeconds / 1000.0), ffmpeg.AVSEEK_FLAG_BACKWARD);
                skipOutputUntilTime = targetTimestamp;
            });
        }

        /// <summary>
        /// Starts the decoding process. The decoding will happen asynchronously in a separate thread. The decoded frames can be retrieved by using <see cref="VideoDecoder.GetDecodedFrames"/>.
        /// </summary>
        public override void StartDecoding()
        {
            // only prepare for decoding if this is our first time starting the decoding process
            if (formatContext == null)
                prepareDecoding();

            decodingTaskCancellationTokenSource = new CancellationTokenSource();
            decodingTask = Task.Factory.StartNew(() => decodingLoop(decodingTaskCancellationTokenSource.Token), decodingTaskCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Stops the decoding process. Optionally waits for the decoder thread to terminate.
        /// </summary>
        /// <param name="waitForDecoderExit">True if this method should wait for the decoder thread to terminate, false otherwise.</param>
        public override void StopDecoding(bool waitForDecoderExit)
        {
            if (decodingTask == null)
                return;

            decodingTaskCancellationTokenSource.Cancel();
            if (waitForDecoderExit)
                decodingTask.Wait();

            decodingTask = null;
            decodingTaskCancellationTokenSource.Dispose();
            decodingTaskCancellationTokenSource = null;

            State = DecoderState.Ready;
        }

        private int readPacket(void* opaque, byte* bufferPtr, int bufferSize)
        {
            if (bufferSize != managedContextBuffer.Length)
                managedContextBuffer = new byte[bufferSize];

            var bytesRead = VideoStream.Read(managedContextBuffer, 0, bufferSize);
            Marshal.Copy(managedContextBuffer, 0, (IntPtr)bufferPtr, bytesRead);
            return bytesRead;
        }

        private long seek(void* opaque, long offset, int whence)
        {
            if (!VideoStream.CanSeek)
                throw new InvalidOperationException("Tried seeking on a video sourced by a non-seekable stream.");

            switch (whence)
            {
                case StdIo.SEEK_CUR:
                    VideoStream.Seek(offset, SeekOrigin.Current);
                    break;

                case StdIo.SEEK_END:
                    VideoStream.Seek(offset, SeekOrigin.End);
                    break;

                case StdIo.SEEK_SET:
                    VideoStream.Seek(offset, SeekOrigin.Begin);
                    break;

                case ffmpeg.AVSEEK_SIZE:
                    return VideoStream.Length;

                default:
                    return -1;
            }

            return VideoStream.Position;
        }

        // sets up libavformat state: creates the AVFormatContext, the frames, etc. to start decoding, but does not actually start the decodingLoop
        private void prepareDecoding()
        {
            const int context_buffer_size = 4096;

            var fcPtr = ffmpeg.avformat_alloc_context();
            formatContext = fcPtr;
            contextBuffer = (byte*)ffmpeg.av_malloc(context_buffer_size);
            managedContextBuffer = new byte[context_buffer_size];
            readPacketCallback = readPacket;
            seekCallback = seek;
            formatContext->pb = ffmpeg.avio_alloc_context(contextBuffer, context_buffer_size, 0, null, readPacketCallback, null, seekCallback);
            if (ffmpeg.avformat_open_input(&fcPtr, "dummy", null, null) < 0)
                throw new Exception("Error opening file.");

            if (ffmpeg.avformat_find_stream_info(formatContext, null) < 0)
                throw new Exception("Could not find stream info.");

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
                        throw new Exception("Could not find codec.");

                    if (ffmpeg.avcodec_open2(stream->codec, codecPtr, null) < 0)
                        throw new Exception("Could not open codec.");

                    frame = ffmpeg.av_frame_alloc();
                    ffmpegFrame = ffmpeg.av_frame_alloc();

                    uncompressedFrameSize = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height, 1);
                    frameRgbBufferPtr = Marshal.AllocHGlobal(uncompressedFrameSize);

                    var dataArr4 = *(byte_ptrArray4*)&ffmpegFrame->data;
                    var linesizeArr4 = *(int_array4*)&ffmpegFrame->linesize;
                    var result = ffmpeg.av_image_fill_arrays(ref dataArr4, ref linesizeArr4, (byte*)frameRgbBufferPtr, AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height, 1);
                    if (result < 0)
                        throw new Exception("Could not fill image arrays");

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

                    if (DecodedFrames.Count < max_pending_frames)
                    {
                        int readFrameResult = ffmpeg.av_read_frame(formatContext, packet);

                        if (readFrameResult >= 0)
                        {
                            state = DecoderState.Running;

                            if (packet->stream_index == stream->index)
                            {
                                if (ffmpeg.avcodec_send_packet(stream->codec, packet) < 0)
                                    throw new Exception("Error sending packet.");

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

                                        if (!AvailableTextures.TryDequeue(out var tex))
                                            tex = new Texture(codecParams.width, codecParams.height, true);

                                        var upload = new ArrayPoolTextureUpload(tex.Width, tex.Height);

                                        // todo: can likely make this more efficient
                                        new Span<Rgba32>(ffmpegFrame->data[0], uncompressedFrameSize / 4).CopyTo(upload.RawData);

                                        tex.SetData(upload);
                                        DecodedFrames.Enqueue(new DecodedFrame { Time = frameTime, Texture = tex });
                                    }

                                    lastDecodedFrameTime = (float)frameTime;
                                }
                            }
                        }
                        else if (readFrameResult == ffmpeg.AVERROR_EOF)
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
            catch (Exception)
            {
                state = DecoderState.Faulted;
            }
            finally
            {
                ffmpeg.av_packet_free(&packet);

                if (state != DecoderState.Faulted)
                    state = DecoderState.Stopped;
            }
        }

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(disposing);

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
        }

        #endregion
    }
}
