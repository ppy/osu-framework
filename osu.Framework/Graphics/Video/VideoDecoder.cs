// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using FFmpeg.AutoGen;
using osuTK;
using osu.Framework.Graphics.Textures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Platform.Linux.Native;
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
        public double Duration { get; private set; }

        /// <summary>
        /// True if the decoder currently does not decode any more frames, false otherwise.
        /// </summary>
        public bool IsRunning => State == DecoderState.Running;

        /// <summary>
        /// True if the decoder has faulted after starting to decode. You can try to restart a failed decoder by invoking <see cref="StartDecoding"/> again.
        /// </summary>
        public bool IsFaulted => State == DecoderState.Faulted;

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
        public bool CanSeek => videoStream?.CanSeek == true;

        /// <summary>
        /// The current state of the decoding process.
        /// </summary>
        public DecoderState State { get; private set; }

        /// <summary>
        /// Determines which hardware acceleration device(s) should be used.
        /// </summary>
        public readonly Bindable<HardwareVideoDecoder> TargetHardwareVideoDecoders = new Bindable<HardwareVideoDecoder>();

        // libav-context-related
        private AVFormatContext* formatContext;
        private AVIOContext* ioContext;
        private AVStream* stream;
        private AVCodecContext* codecContext;
        private SwsContext* swsContext;

        private avio_alloc_context_read_packet readPacketCallback;
        private avio_alloc_context_seek seekCallback;

        private bool inputOpened;
        private bool isDisposed;
        private bool hwDecodingAllowed = true;
        private Stream videoStream;

        private double timeBaseInSeconds;

        // active decoder state
        private volatile float lastDecodedFrameTime;

        private Task decodingTask;
        private CancellationTokenSource decodingTaskCancellationTokenSource;

        private double? skipOutputUntilTime;

        private readonly IRenderer renderer;

        private readonly ConcurrentQueue<DecodedFrame> decodedFrames;
        private readonly ConcurrentQueue<Action> decoderCommands;

        private readonly ConcurrentQueue<Texture> availableTextures;

        private ObjectHandle<VideoDecoder> handle;

        private readonly FFmpegFuncs ffmpeg;

        internal bool Looping;

        static VideoDecoder()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // FFmpeg.AutoGen doesn't load libraries as RTLD_GLOBAL, so we must load them ourselves to fix inter-library dependencies
                // otherwise they would fallback to the system-installed libraries that can differ in version installed.
                Library.Load("libavutil.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
                Library.Load("libavcodec.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
                Library.Load("libavformat.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
                Library.Load("libavfilter.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
                Library.Load("libswscale.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
        }

        /// <summary>
        /// Creates a new video decoder that decodes the given video file.
        /// </summary>
        /// <param name="renderer">The renderer to display the video.</param>
        /// <param name="filename">The path to the file that should be decoded.</param>
        public VideoDecoder(IRenderer renderer, string filename)
            : this(renderer, File.OpenRead(filename))
        {
        }

        /// <summary>
        /// Creates a new video decoder that decodes the given video stream.
        /// </summary>
        /// <param name="renderer">The renderer to display the video.</param>
        /// <param name="videoStream">The stream that should be decoded.</param>
        public VideoDecoder(IRenderer renderer, Stream videoStream)
        {
            ffmpeg = CreateFuncs();

            this.renderer = renderer;
            this.videoStream = videoStream;
            if (!videoStream.CanRead)
                throw new InvalidOperationException($"The given stream does not support reading. A stream used for a {nameof(VideoDecoder)} must support reading.");

            State = DecoderState.Ready;
            decodedFrames = new ConcurrentQueue<DecodedFrame>();
            decoderCommands = new ConcurrentQueue<Action>();
            availableTextures = new ConcurrentQueue<Texture>(); // TODO: use "real" object pool when there's some public pool supporting disposables
            handle = new ObjectHandle<VideoDecoder>(this, GCHandleType.Normal);

            TargetHardwareVideoDecoders.BindValueChanged(_ =>
            {
                // ignore if decoding wasn't initialized yet.
                if (formatContext == null)
                    return;

                decoderCommands.Enqueue(recreateCodecContext);
            });
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
                ffmpeg.avcodec_flush_buffers(codecContext);
                ffmpeg.av_seek_frame(formatContext, stream->index, (long)(targetTimestamp / timeBaseInSeconds / 1000.0), AGffmpeg.AVSEEK_FLAG_BACKWARD);
                skipOutputUntilTime = targetTimestamp;
                State = DecoderState.Ready;
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
                f.Texture.FlushUploads();
                availableTextures.Enqueue(f.Texture);
            }
        }

        /// <summary>
        /// Starts the decoding process. The decoding will happen asynchronously in a separate thread. The decoded frames can be retrieved by using <see cref="GetDecodedFrames"/>.
        /// </summary>
        public void StartDecoding()
        {
            if (decodingTask != null)
                throw new InvalidOperationException($"Cannot start decoding once already started. Call {nameof(StopDecodingAsync)} first.");

            // only prepare for decoding if this is our first time starting the decoding process
            if (formatContext == null)
            {
                try
                {
                    prepareDecoding();
                    recreateCodecContext();
                }
                catch (Exception e)
                {
                    Logger.Log($"VideoDecoder faulted: {e}");
                    State = DecoderState.Faulted;
                    return;
                }
            }

            decodingTaskCancellationTokenSource = new CancellationTokenSource();
            decodingTask = Task.Factory.StartNew(() => decodingLoop(decodingTaskCancellationTokenSource.Token), decodingTaskCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Stops the decoding process.
        /// </summary>
        public Task StopDecodingAsync()
        {
            if (decodingTask == null)
                return Task.CompletedTask;

            decodingTaskCancellationTokenSource.Cancel();

            return decodingTask.ContinueWith(_ =>
            {
                decodingTask = null;
                decodingTaskCancellationTokenSource.Dispose();
                decodingTaskCancellationTokenSource = null;

                State = DecoderState.Ready;
            });
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

        // https://en.wikipedia.org/wiki/YCbCr
        public Matrix3 GetConversionMatrix()
        {
            if (codecContext == null)
                return Matrix3.Zero;

            switch (codecContext->colorspace)
            {
                case AVColorSpace.AVCOL_SPC_BT709:
                    return new Matrix3(1.164f, 1.164f, 1.164f,
                        0.000f, -0.213f, 2.112f,
                        1.793f, -0.533f, 0.000f);

                case AVColorSpace.AVCOL_SPC_UNSPECIFIED:
                case AVColorSpace.AVCOL_SPC_SMPTE170M:
                case AVColorSpace.AVCOL_SPC_SMPTE240M:
                default:
                    return new Matrix3(1.164f, 1.164f, 1.164f,
                        0.000f, -0.392f, 2.017f,
                        1.596f, -0.813f, 0.000f);
            }
        }

        [MonoPInvokeCallback(typeof(avio_alloc_context_read_packet))]
        private static int readPacket(void* opaque, byte* bufferPtr, int bufferSize)
        {
            var handle = new ObjectHandle<VideoDecoder>((IntPtr)opaque);
            if (!handle.GetTarget(out VideoDecoder decoder))
                return 0;

            var span = new Span<byte>(bufferPtr, bufferSize);

            return decoder.videoStream.Read(span);
        }

        [MonoPInvokeCallback(typeof(avio_alloc_context_seek))]
        private static long streamSeekCallbacks(void* opaque, long offset, int whence)
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
            readPacketCallback = readPacket;
            seekCallback = streamSeekCallbacks;
            // we shouldn't keep a reference to this buffer as it can be freed and replaced by the native libs themselves.
            // https://ffmpeg.org/doxygen/4.1/aviobuf_8c.html#a853f5149136a27ffba3207d8520172a5
            byte* contextBuffer = (byte*)ffmpeg.av_malloc(context_buffer_size);

            ioContext = ffmpeg.avio_alloc_context(contextBuffer, context_buffer_size, 0, (void*)handle.Handle, readPacketCallback, null, seekCallback);

            var fcPtr = ffmpeg.avformat_alloc_context();
            formatContext = fcPtr;
            formatContext->pb = ioContext;
            formatContext->flags |= AGffmpeg.AVFMT_FLAG_GENPTS; // required for most HW decoders as they only read `pts`

            int openInputResult = ffmpeg.avformat_open_input(&fcPtr, "dummy", null, null);
            inputOpened = openInputResult >= 0;
            if (!inputOpened)
                throw new InvalidOperationException($"Error opening file or stream: {getErrorMessage(openInputResult)}");

            int findStreamInfoResult = ffmpeg.avformat_find_stream_info(formatContext, null);
            if (findStreamInfoResult < 0)
                throw new InvalidOperationException($"Error finding stream info: {getErrorMessage(findStreamInfoResult)}");

            int streamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            if (streamIndex < 0)
                throw new InvalidOperationException($"Couldn't find video stream: {getErrorMessage(streamIndex)}");

            stream = formatContext->streams[streamIndex];
            timeBaseInSeconds = stream->time_base.GetValue();

            if (stream->duration > 0)
                Duration = stream->duration * timeBaseInSeconds * 1000.0;
            else
                Duration = formatContext->duration / (double)AGffmpeg.AV_TIME_BASE * 1000.0;
        }

        private void recreateCodecContext()
        {
            if (stream == null)
                return;

            var codecParams = *stream->codecpar;
            var targetHwDecoders = hwDecodingAllowed ? TargetHardwareVideoDecoders.Value : HardwareVideoDecoder.None;
            bool openSuccessful = false;

            foreach (var (decoder, hwDeviceType) in GetAvailableDecoders(formatContext->iformat, codecParams.codec_id, targetHwDecoders))
            {
                // free context in case it was allocated in a previous iteration or recreate call.
                if (codecContext != null)
                {
                    fixed (AVCodecContext** ptr = &codecContext)
                        ffmpeg.avcodec_free_context(ptr);
                }

                codecContext = ffmpeg.avcodec_alloc_context3(decoder.Pointer);
                codecContext->pkt_timebase = stream->time_base;

                if (codecContext == null)
                {
                    Logger.Log($"Couldn't allocate codec context. Codec: {decoder.Name}");
                    continue;
                }

                int paramCopyResult = ffmpeg.avcodec_parameters_to_context(codecContext, &codecParams);

                if (paramCopyResult < 0)
                {
                    Logger.Log($"Couldn't copy codec parameters from {decoder.Name}: {getErrorMessage(paramCopyResult)}");
                    continue;
                }

                // initialize hardware decode context.
                if (hwDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                {
                    int hwDeviceCreateResult = ffmpeg.av_hwdevice_ctx_create(&codecContext->hw_device_ctx, hwDeviceType, null, null, 0);

                    if (hwDeviceCreateResult < 0)
                    {
                        Logger.Log($"Couldn't create hardware video decoder context {hwDeviceType} for codec {decoder.Name}: {getErrorMessage(hwDeviceCreateResult)}");
                        continue;
                    }

                    Logger.Log($"Successfully opened hardware video decoder context {hwDeviceType} for codec {decoder.Name}");
                }

                int openCodecResult = ffmpeg.avcodec_open2(codecContext, decoder.Pointer, null);

                if (openCodecResult < 0)
                {
                    Logger.Log($"Error trying to open {decoder.Name} codec: {getErrorMessage(openCodecResult)}");
                    continue;
                }

                Logger.Log($"Successfully initialized decoder: {decoder.Name}");

                openSuccessful = true;
                break;
            }

            if (!openSuccessful)
                throw new InvalidOperationException("No usable decoder found");
        }

        private void decodingLoop(CancellationToken cancellationToken)
        {
            var packet = ffmpeg.av_packet_alloc();
            var receiveFrame = ffmpeg.av_frame_alloc();

            const int max_pending_frames = 3;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    switch (State)
                    {
                        case DecoderState.Ready:
                        case DecoderState.Running:
                            if (decodedFrames.Count < max_pending_frames)
                            {
                                decodeNextFrame(packet, receiveFrame);
                            }
                            else
                            {
                                // wait until existing buffers are consumed.
                                State = DecoderState.Ready;
                                Thread.Sleep(1);
                            }

                            break;

                        case DecoderState.EndOfStream:
                            // While at the end of the stream, avoid attempting to read further as this comes with a non-negligible overhead.
                            // A Seek() operation will trigger a state change, allowing decoding to potentially start again.
                            Thread.Sleep(50);
                            break;

                        default:
                            Debug.Fail($"Video decoder should never be in a \"{State}\" state during decode.");
                            return;
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
                Logger.Error(e, "VideoDecoder faulted");
                State = DecoderState.Faulted;
            }
            finally
            {
                ffmpeg.av_packet_free(&packet);
                ffmpeg.av_frame_free(&receiveFrame);

                if (State != DecoderState.Faulted)
                    State = DecoderState.Stopped;
            }
        }

        private void decodeNextFrame(AVPacket* packet, AVFrame* receiveFrame)
        {
            // read data from input into AVPacket.
            // only read if the packet is empty, otherwise we would overwrite what's already there which can lead to visual glitches.
            int readFrameResult = 0;
            if (packet->buf == null)
                readFrameResult = ffmpeg.av_read_frame(formatContext, packet);

            if (readFrameResult >= 0)
            {
                State = DecoderState.Running;

                bool unrefPacket = true;

                if (packet->stream_index == stream->index)
                {
                    int sendPacketResult = sendPacket(receiveFrame, packet);

                    // keep the packet data for next frame if we didn't send it successfully.
                    if (sendPacketResult == -AGffmpeg.EAGAIN)
                    {
                        unrefPacket = false;
                    }
                }

                if (unrefPacket)
                    ffmpeg.av_packet_unref(packet);
            }
            else if (readFrameResult == AGffmpeg.AVERROR_EOF)
            {
                // Flush decoder.
                sendPacket(receiveFrame, null);

                if (Looping)
                {
                    Seek(0);
                }
                else
                {
                    // This marks the video stream as no longer relevant (until a future potential Seek operation).
                    State = DecoderState.EndOfStream;
                }
            }
            else if (readFrameResult == -AGffmpeg.EAGAIN)
            {
                State = DecoderState.Ready;
                Thread.Sleep(1);
            }
            else
            {
                Logger.Log($"Failed to read data into avcodec packet: {getErrorMessage(readFrameResult)}");
                Thread.Sleep(1);
            }
        }

        private int sendPacket(AVFrame* receiveFrame, AVPacket* packet)
        {
            // send the packet for decoding.
            int sendPacketResult = ffmpeg.avcodec_send_packet(codecContext, packet);

            // Note: EAGAIN can be returned if there's too many pending frames, which we have to read,
            // otherwise we would get stuck in an infinite loop.
            if (sendPacketResult == 0 || sendPacketResult == -AGffmpeg.EAGAIN)
            {
                readDecodedFrames(receiveFrame);
            }
            else
            {
                Logger.Log($"Failed to send avcodec packet: {getErrorMessage(sendPacketResult)}");
                tryDisableHwDecoding(sendPacketResult);
            }

            return sendPacketResult;
        }

        private readonly ConcurrentQueue<FFmpegFrame> hwTransferFrames = new ConcurrentQueue<FFmpegFrame>();
        private void returnHwTransferFrame(FFmpegFrame frame) => hwTransferFrames.Enqueue(frame);

        private void readDecodedFrames(AVFrame* receiveFrame)
        {
            while (true)
            {
                int receiveFrameResult = ffmpeg.avcodec_receive_frame(codecContext, receiveFrame);

                if (receiveFrameResult < 0)
                {
                    if (receiveFrameResult != -AGffmpeg.EAGAIN && receiveFrameResult != AGffmpeg.AVERROR_EOF)
                    {
                        Logger.Log($"Failed to receive frame from avcodec: {getErrorMessage(receiveFrameResult)}");
                        tryDisableHwDecoding(receiveFrameResult);
                    }

                    break;
                }

                // use `best_effort_timestamp` as it can be more accurate if timestamps from the source file (pts) are broken.
                // but some HW codecs don't set it in which case fallback to `pts`
                long frameTimestamp = receiveFrame->best_effort_timestamp != AGffmpeg.AV_NOPTS_VALUE ? receiveFrame->best_effort_timestamp : receiveFrame->pts;

                double frameTime = (frameTimestamp - stream->start_time) * timeBaseInSeconds * 1000;

                if (skipOutputUntilTime > frameTime)
                    continue;

                // get final frame.
                FFmpegFrame frame;

                if (((AVPixelFormat)receiveFrame->format).IsHardwarePixelFormat())
                {
                    // transfer data from HW decoder to RAM.
                    if (!hwTransferFrames.TryDequeue(out var hwTransferFrame))
                        hwTransferFrame = new FFmpegFrame(ffmpeg, returnHwTransferFrame);

                    // WARNING: frames from `av_hwframe_transfer_data` have their timestamps set to AV_NOPTS_VALUE instead of real values.
                    // if you need to use them later, take them from `receiveFrame`.
                    int transferResult = ffmpeg.av_hwframe_transfer_data(hwTransferFrame.Pointer, receiveFrame, 0);

                    if (transferResult < 0)
                    {
                        Logger.Log($"Failed to transfer frame from HW decoder: {getErrorMessage(transferResult)}");
                        tryDisableHwDecoding(transferResult);

                        hwTransferFrame.Dispose();
                        continue;
                    }

                    frame = hwTransferFrame;
                }
                else
                {
                    // copy data to a new AVFrame so that `receiveFrame` can be reused.
                    frame = new FFmpegFrame(ffmpeg);
                    ffmpeg.av_frame_move_ref(frame.Pointer, receiveFrame);
                }

                lastDecodedFrameTime = (float)frameTime;

                // Note: this is the pixel format that `VideoTexture` expects internally
                frame = ensureFramePixelFormat(frame, AVPixelFormat.AV_PIX_FMT_YUV420P);
                if (frame == null)
                    continue;

                if (!availableTextures.TryDequeue(out var tex))
                    tex = renderer.CreateVideoTexture(frame.Pointer->width, frame.Pointer->height);

                var upload = new VideoTextureUpload(frame);

                // We do not support videos with transparency at this point, so the upload's opacity as well as the texture's opacity is always opaque.
                tex.SetData(upload, Opacity.Opaque);
                decodedFrames.Enqueue(new DecodedFrame { Time = frameTime, Texture = tex });
            }
        }

        private readonly ConcurrentQueue<FFmpegFrame> scalerFrames = new ConcurrentQueue<FFmpegFrame>();
        private void returnScalerFrame(FFmpegFrame frame) => scalerFrames.Enqueue(frame);

        [CanBeNull]
        private FFmpegFrame ensureFramePixelFormat(FFmpegFrame frame, AVPixelFormat targetPixelFormat)
        {
            if (frame.PixelFormat == targetPixelFormat)
                return frame;

            int width = frame.Pointer->width;
            int height = frame.Pointer->height;

            swsContext = ffmpeg.sws_getCachedContext(
                swsContext,
                width, height, frame.PixelFormat,
                width, height, targetPixelFormat,
                1, null, null, null);

            if (!scalerFrames.TryDequeue(out var scalerFrame))
                scalerFrame = new FFmpegFrame(ffmpeg, returnScalerFrame);

            // (re)initialize the scaler frame if needed.
            if (scalerFrame.PixelFormat != targetPixelFormat || scalerFrame.Pointer->width != width || scalerFrame.Pointer->height != height)
            {
                ffmpeg.av_frame_unref(scalerFrame.Pointer);

                // Note: this field determines the scaler's output pix format.
                scalerFrame.PixelFormat = targetPixelFormat;
                scalerFrame.Pointer->width = width;
                scalerFrame.Pointer->height = height;

                int getBufferResult = ffmpeg.av_frame_get_buffer(scalerFrame.Pointer, 0);

                if (getBufferResult < 0)
                {
                    Logger.Log($"Failed to allocate SWS frame buffer: {getErrorMessage(getBufferResult)}");

                    scalerFrame.Dispose();
                    frame.Return();
                    return null;
                }
            }

            int scalerResult = ffmpeg.sws_scale(
                swsContext,
                frame.Pointer->data, frame.Pointer->linesize, 0, height,
                scalerFrame.Pointer->data, scalerFrame.Pointer->linesize);

            // return the original frame regardless of the scaler result.
            frame.Return();

            if (scalerResult < 0)
            {
                Logger.Log($"Failed to scale frame: {getErrorMessage(scalerResult)}");

                scalerFrame.Dispose();
                return null;
            }

            return scalerFrame;
        }

        private void tryDisableHwDecoding(int errorCode)
        {
            if (!hwDecodingAllowed || TargetHardwareVideoDecoders.Value == HardwareVideoDecoder.None || codecContext == null || codecContext->hw_device_ctx == null)
                return;

            hwDecodingAllowed = false;

            if (errorCode == -AGffmpeg.ENOMEM)
            {
                Logger.Log("Disabling hardware decoding of all videos due to a lack of memory");
                TargetHardwareVideoDecoders.Value = HardwareVideoDecoder.None;

                // `recreateCodecContext` will be called by the bindable hook
            }
            else
            {
                Logger.Log("Disabling hardware decoding of the current video due to an unexpected error");

                decoderCommands.Enqueue(recreateCodecContext);
            }
        }

        private string getErrorMessage(int errorCode)
        {
            const ulong buffer_size = 256;
            byte[] buffer = new byte[buffer_size];

            int strErrorCode;

            fixed (byte* bufPtr = buffer)
            {
                strErrorCode = ffmpeg.av_strerror(errorCode, bufPtr, buffer_size);
            }

            if (strErrorCode < 0)
                return $"{errorCode} (av_strerror failed with code {strErrorCode})";

            int messageLength = Math.Max(0, Array.IndexOf(buffer, (byte)0));
            return $"{Encoding.ASCII.GetString(buffer[..messageLength])} ({errorCode})";
        }

        /// <remarks>
        /// Returned HW devices are not guaranteed to be available on the current machine, they only represent what the loaded FFmpeg libraries support.
        /// </remarks>
        protected virtual IEnumerable<(FFmpegCodec codec, AVHWDeviceType hwDeviceType)> GetAvailableDecoders(
            AVInputFormat* inputFormat,
            AVCodecID codecId,
            HardwareVideoDecoder targetHwDecoders
        )
        {
            var comparer = new AVHWDeviceTypePerformanceComparer();
            var codecs = new Lists.SortedList<(FFmpegCodec, AVHWDeviceType hwDeviceType)>((x, y) => comparer.Compare(x.hwDeviceType, y.hwDeviceType));
            FFmpegCodec firstCodec = null;

            void* iterator = null;

            while (true)
            {
                var avCodec = ffmpeg.av_codec_iterate(&iterator);

                if (avCodec == null) break;

                var codec = new FFmpegCodec(ffmpeg, avCodec);
                if (codec.Id != codecId || !codec.IsDecoder) continue;

                firstCodec ??= codec;

                if (targetHwDecoders == HardwareVideoDecoder.None)
                    break;

                foreach (var hwDeviceType in codec.SupportedHwDeviceTypes.Value)
                {
                    var hwVideoDecoder = hwDeviceType.ToHardwareVideoDecoder();

                    if (!hwVideoDecoder.HasValue || !targetHwDecoders.HasFlagFast(hwVideoDecoder.Value))
                        continue;

                    codecs.Add((codec, hwDeviceType));
                }
            }

            // default to the first codec that we found with no HW devices.
            // The first codec is what FFmpeg's `avcodec_find_decoder` would return so this way we'll automatically fallback to that.
            if (firstCodec != null)
                codecs.Add((firstCodec, AVHWDeviceType.AV_HWDEVICE_TYPE_NONE));

            return codecs;
        }

        protected virtual FFmpegFuncs CreateFuncs()
        {
            // other frameworks should handle native libraries themselves
#if NET6_0_OR_GREATER
            AGffmpeg.GetOrLoadLibrary = name =>
            {
                int version = AGffmpeg.LibraryVersionMap[name];

                string libraryName = null;

                // "lib" prefix and extensions are resolved by .net core
                switch (RuntimeInfo.OS)
                {
                    case RuntimeInfo.Platform.macOS:
                        libraryName = $"{name}.{version}";
                        break;

                    case RuntimeInfo.Platform.Windows:
                        libraryName = $"{name}-{version}";
                        break;

                    case RuntimeInfo.Platform.Linux:
                        libraryName = name;
                        break;
                }

                return NativeLibrary.Load(libraryName, System.Reflection.Assembly.GetEntryAssembly(), DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories);
            };
#endif

            return new FFmpegFuncs
            {
                av_frame_alloc = AGffmpeg.av_frame_alloc,
                av_frame_free = AGffmpeg.av_frame_free,
                av_frame_unref = AGffmpeg.av_frame_unref,
                av_frame_move_ref = AGffmpeg.av_frame_move_ref,
                av_frame_get_buffer = AGffmpeg.av_frame_get_buffer,
                av_strdup = AGffmpeg.av_strdup,
                av_strerror = AGffmpeg.av_strerror,
                av_malloc = AGffmpeg.av_malloc,
                av_freep = AGffmpeg.av_freep,
                av_packet_alloc = AGffmpeg.av_packet_alloc,
                av_packet_unref = AGffmpeg.av_packet_unref,
                av_packet_free = AGffmpeg.av_packet_free,
                av_read_frame = AGffmpeg.av_read_frame,
                av_seek_frame = AGffmpeg.av_seek_frame,
                av_hwdevice_ctx_create = AGffmpeg.av_hwdevice_ctx_create,
                av_hwframe_transfer_data = AGffmpeg.av_hwframe_transfer_data,
                av_codec_iterate = AGffmpeg.av_codec_iterate,
                av_codec_is_decoder = AGffmpeg.av_codec_is_decoder,
                avcodec_get_hw_config = AGffmpeg.avcodec_get_hw_config,
                avcodec_alloc_context3 = AGffmpeg.avcodec_alloc_context3,
                avcodec_free_context = AGffmpeg.avcodec_free_context,
                avcodec_parameters_to_context = AGffmpeg.avcodec_parameters_to_context,
                avcodec_open2 = AGffmpeg.avcodec_open2,
                avcodec_receive_frame = AGffmpeg.avcodec_receive_frame,
                avcodec_send_packet = AGffmpeg.avcodec_send_packet,
                avcodec_flush_buffers = AGffmpeg.avcodec_flush_buffers,
                avformat_alloc_context = AGffmpeg.avformat_alloc_context,
                avformat_close_input = AGffmpeg.avformat_close_input,
                avformat_find_stream_info = AGffmpeg.avformat_find_stream_info,
                avformat_open_input = AGffmpeg.avformat_open_input,
                av_find_best_stream = AGffmpeg.av_find_best_stream,
                avio_alloc_context = AGffmpeg.avio_alloc_context,
                avio_context_free = AGffmpeg.avio_context_free,
                sws_freeContext = AGffmpeg.sws_freeContext,
                sws_getCachedContext = AGffmpeg.sws_getCachedContext,
                sws_scale = AGffmpeg.sws_scale
            };
        }

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

            decoderCommands.Clear();

            StopDecodingAsync().ContinueWith(_ =>
            {
                if (formatContext != null && inputOpened)
                {
                    fixed (AVFormatContext** ptr = &formatContext)
                        ffmpeg.avformat_close_input(ptr);
                }

                if (codecContext != null)
                {
                    fixed (AVCodecContext** ptr = &codecContext)
                        ffmpeg.avcodec_free_context(ptr);
                }

                seekCallback = null;
                readPacketCallback = null;

                videoStream.Dispose();
                videoStream = null;

                if (swsContext != null)
                    ffmpeg.sws_freeContext(swsContext);

                while (decodedFrames.TryDequeue(out var f))
                {
                    f.Texture.FlushUploads();
                    f.Texture.Dispose();
                }

                while (availableTextures.TryDequeue(out var t))
                    t.Dispose();

                while (hwTransferFrames.TryDequeue(out var hwF))
                    hwF.Dispose();

                while (scalerFrames.TryDequeue(out var sf))
                    sf.Dispose();

                handle.Dispose();
            });
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
