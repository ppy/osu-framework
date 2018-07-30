// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a sprite that plays back a video from a stream or a file.
    /// </summary>
    public unsafe class VideoSprite : Sprite
    {
        /// <summary>
        /// The duration of the video. Can only be queried after the VideoSprite has loaded. This value may be an estimate by FFmpeg, depending on the video loaded.
        /// </summary>
        public double Duration => formatContext->duration / AVUtil.AV_TIME_BASE * 1000;

        /// <summary>
        /// True if the video has finished playing, false otherwise.
        /// </summary>
        public bool FinishedPlaying => Loop ? false : PlaybackPosition > Duration;

        /// <summary>
        /// True if the video should loop after finishing its playback, false otherwise.
        /// </summary>
        public bool Loop { get; set; }

        private double? baseTime;
        /// <summary>
        /// The current position of the video playback. The playback position is automatically calculated based on the clock of the VideoSprite. If you want to seek ahead or jump backwards you have to change the <see cref="Timing.IClock.CurrentTime"/> of the clock of this VideoSprite.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                if (!baseTime.HasValue)
                    return 0;

                if (Loop)
                    return (Clock.CurrentTime - baseTime.Value) % Duration;
                else
                    return Math.Min(Clock.CurrentTime - baseTime.Value, Duration);
            }
        }

        /// <summary>
        /// The cutoff, in number of frames of video, that the decoder has to be lagging behind to make the VideoSprite stop displaying newly decoded frames until the decoder has caught up. Setting this to null will cause the VideoSprite to always display the decoded frame that is closest to the current playback position, regardless of decoder delay.
        /// </summary>
        public int? HideCutoff;

        /// <summary>
        /// True if during an active <see cref="HideCutoff"/> the last frame that did not fall under the <see cref="HideCutoff"/>-threshold should be displayed until the decoder catches up. Setting this to false will make the VideoSprite clear its display during a <see cref="HideCutoff"/>.
        /// </summary>
        public bool ShowLastFrameDuringHideCutoff;

        /// <summary>
        /// The number of frames that should be preloaded by the decoder. Larger values means higher memory usage, but can avoid falling into <see cref="HideCutoff"/> for small adjustments to the <see cref="PlaybackPosition"/> and can reduce CPU usage on short looping clips.
        /// </summary>
        public int NumberOfPreloadedFrames = 60;

        /// <summary>
        /// True if this VideoSprites decoding process has faulted.
        /// </summary>
        public bool IsFaulted { get; private set; }

        // ffmpeg-context related data
        private double timeBaseInSeconds;
        private AVFormatContext* formatContext;
        private byte* contextBuffer;
        private const int context_buffer_size = 4096;
        private byte[] managedContextBuffer;
        private AVStream* stream;
        private AVCodecParameters codecParams;
        private AVFormat.ReadPacketCallback readPacketCallback;
        private AVFormat.SeekCallback seekCallback;

        private Stream videoStream;

        // unmanaged frame data
        private AVFrame* frame;
        private AVFrame* frameRgb;
        private int uncompressedFrameSize;
        private IntPtr frameRgbBufferPtr;

        // data for decoding
        private struct DecodedFrame
        {
            public double Time { get; set; }

            public Texture Texture { get; set; }
        }

        private Thread decodingThread;

        private bool decodeFrames;
        private ConcurrentQueue<DecodedFrame> decodedFrames;
        private ConcurrentQueue<Action> decoderCommands;

        private volatile float lastDecodedFrameTime;

        private readonly SortedList<DecodedFrame> availableFrames;
        private int currentFrameIndex;

        private bool isDisposed;

        internal float LastDecodedFrameTime => lastDecodedFrameTime;

        internal int CurrentFrameIndex => currentFrameIndex;

        internal double CurrentFrameTime => currentFrameIndex < availableFrames.Count ? availableFrames[currentFrameIndex].Time : 0;

        internal int AvailableFrames => availableFrames.Count;

        private VideoSprite()
        {
            decodedFrames = new ConcurrentQueue<DecodedFrame>();
            availableFrames = new SortedList<DecodedFrame>((left, right) => left.Time.CompareTo(right.Time));
            decoderCommands = new ConcurrentQueue<Action>();
        }

        public VideoSprite(Stream videoStream)
            : this()
        {
            this.videoStream = videoStream;
            if (!videoStream.CanRead)
                throw new InvalidOperationException($"The given stream does not support reading. A stream used for a {nameof(VideoSprite)} must support reading.");
        }

        public VideoSprite(string filename)
            : this(File.OpenRead(filename))
        {
        }

        private int readPacket(void* opaque, byte* buf, int buf_size)
        {
            if (buf_size != managedContextBuffer.Length)
                managedContextBuffer = new byte[buf_size];

            var bytesRead = videoStream.Read(managedContextBuffer, 0, buf_size);
            Marshal.Copy(managedContextBuffer, 0, (IntPtr)buf, bytesRead);
            return bytesRead;
        }

        private long seek(void* opaque, long offset, int whence)
        {
            if (!videoStream.CanSeek)
                throw new InvalidOperationException("Tried seeking on a video sourced by a non-seekable stream.");

            switch (whence)
            {
                case StdIo.SEEK_CUR:
                    videoStream.Seek(offset, SeekOrigin.Current);
                    break;
                case StdIo.SEEK_END:
                    videoStream.Seek(offset, SeekOrigin.End);
                    break;
                case StdIo.SEEK_SET:
                    videoStream.Seek(offset, SeekOrigin.Begin);
                    break;
                case AVFormat.AVSEEK_SIZE:
                    return videoStream.Length;
                default:
                    return -1;
            }
            return videoStream.Position;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var fcPtr = AVFormat.avformat_alloc_context();
            formatContext = fcPtr;
            contextBuffer = AVUtil.av_malloc(context_buffer_size);
            managedContextBuffer = new byte[context_buffer_size];
            readPacketCallback = readPacket;
            seekCallback = seek;
            formatContext->pb = AVFormat.avio_alloc_context(contextBuffer, context_buffer_size, 0, null, readPacketCallback, IntPtr.Zero, seekCallback);
            if (AVFormat.avformat_open_input(&fcPtr, "dummy", IntPtr.Zero, IntPtr.Zero) < 0)
                throw new Exception("Error opening file.");

            if (AVFormat.avformat_find_stream_info((IntPtr)formatContext, IntPtr.Zero) < 0)
                throw new Exception("Could not find stream info.");

            var nStreams = formatContext->nb_streams;
            for (var i = 0; i < nStreams; ++i)
            {
                stream = formatContext->streams[i];

                codecParams = *stream->codecpar;
                if (codecParams.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    timeBaseInSeconds = AVUtil.av_q2d(stream->time_base);
                    var codecPtr = AVCodec.avcodec_find_decoder(codecParams.codec_id);
                    if (codecPtr == IntPtr.Zero)
                        throw new Exception("Could not find codec.");


                    if (AVCodec.avcodec_open2(stream->codec, codecPtr, IntPtr.Zero) < 0)
                        throw new Exception("Could not open codec.");

                    frame = AVUtil.av_frame_alloc();
                    frameRgb = AVUtil.av_frame_alloc();

                    uncompressedFrameSize = AVUtil.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height);
                    frameRgbBufferPtr = Marshal.AllocHGlobal(uncompressedFrameSize);

                    var result = AVUtil.av_image_fill_arrays(&frameRgb->data0, &frameRgb->linesize[0], frameRgbBufferPtr, AVPixelFormat.AV_PIX_FMT_RGBA, codecParams.width, codecParams.height, 1);
                    if (result < 0)
                        throw new Exception("Could not fill image arrays");

                    decodingThread = new Thread(decodingLoop)
                    {
                        IsBackground = true
                    };
                    decodingThread.Start();
                    break;
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!baseTime.HasValue)
                baseTime = Clock.CurrentTime;

            // ensures we do not try to seek with the decoder if the underlying stream does not support seeking (e.g. for network streams)
            if (videoStream.CanSeek)
            {
                // commented out, because seamless looping seems to work without this predictive jump. might want to enable this for weaker machines that cant immediately catch up
                // after seeking back to the beginning and need a bit of a grace period.
                /*if (Loop && PlaybackPosition >= Duration - (NumberOfPreloadedFrames / 2) * 1000.0 / AVUtil.av_q2d(stream->avg_frame_rate))
                {
                    decoderCommands.Enqueue(() => AVFormat.av_seek_frame(formatContext, stream->index, 0, AVFormat.AVSEEK_FLAG_BACKWARD));
                }*/
                // we are before the earliest frame of our buffer, and the last frame we decoded is later than our current playback position
                // this means (because lastDecodedFrameTime only ever increments) that we will never normally decode a frame that is useful to our current playback
                // position. Thus, we need to seek to an earlier frame.
                if (availableFrames.Count > 0 && availableFrames[0].Time > PlaybackPosition && lastDecodedFrameTime > PlaybackPosition)
                {
                    decoderCommands.Enqueue(() => AVFormat.av_seek_frame(formatContext, stream->index, (long)(PlaybackPosition / timeBaseInSeconds / 1000.0), AVFormat.AVSEEK_FLAG_BACKWARD));
                    availableFrames.Clear();
                }
                // the current playback position demands that we are more than 2 seconds further than the last available frame, so we should seek forward
                else if (availableFrames.Count >= 5 && PlaybackPosition > (availableFrames[availableFrames.Count - 1].Time + 2000.0))
                {
                    decoderCommands.Enqueue(() => AVFormat.av_seek_frame(formatContext, stream->index, (long)(PlaybackPosition / timeBaseInSeconds / 1000.0), AVFormat.AVSEEK_FLAG_BACKWARD));
                    availableFrames.Clear();
                }
            }

            while (decodedFrames.TryDequeue(out var newFrame))
            {
                availableFrames.Add(newFrame);
                if (availableFrames.Count >= NumberOfPreloadedFrames)
                    decodeFrames = false;
            }
            decodeFrames = availableFrames.Count < NumberOfPreloadedFrames;

            var oldIndex = currentFrameIndex;
            var index = availableFrames.BinarySearch(new DecodedFrame { Time = PlaybackPosition });
            if (index < 0)
                index = ~index;
            if (index < availableFrames.Count)
            {
                currentFrameIndex = index;
                var isInHideCutoff = HideCutoff.HasValue && Math.Abs(availableFrames[index].Time - PlaybackPosition) > HideCutoff * 1000.0 / AVUtil.av_q2d(stream->avg_frame_rate);
                if (isInHideCutoff)
                    Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
                else
                    Texture = availableFrames[index].Texture;
            }
            else if (availableFrames.Count > 0)
            {
                currentFrameIndex = availableFrames.Count - 1;
                var isInHideCutoff = HideCutoff.HasValue && Math.Abs(availableFrames[availableFrames.Count - 1].Time - PlaybackPosition) > HideCutoff * 1000.0 / AVUtil.av_q2d(stream->avg_frame_rate);
                if (isInHideCutoff)
                    Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
                else
                    Texture = availableFrames[availableFrames.Count - 1].Texture;
            }
            else
            {
                currentFrameIndex = 0;
                Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
            }

            if (currentFrameIndex > NumberOfPreloadedFrames / 2)
            {
                int nRemovedFrames = Math.Max(1, NumberOfPreloadedFrames / 4);
                availableFrames.RemoveRange(0, nRemovedFrames);
                currentFrameIndex -= nRemovedFrames;
            }
        }

        private void decodingLoop()
        {
            var packet = AVCodec.av_packet_alloc();
            // this should be massively reduced to something like 5-10, currently there is an issue with texture uploads not completing
            // in a predictable way though, which can cause huge overallocations. Going past the bufferstacks limit essentially breaks
            // video playback (~several GB memory usage building up very quickly accompanied by unacceptable framerates).
            var bufferStack = new BufferStack<byte>(300);
            IntPtr swsCtx = IntPtr.Zero;
            try
            {
                while (true)
                {
                    if (isDisposed)
                        return;

                    int readFrameResult = AVFormat.av_read_frame(formatContext, packet);
                    if (readFrameResult >= 0)
                    {
                        if (packet->stream_index == stream->index)
                        {
                            if (AVCodec.avcodec_send_packet(stream->codec, packet) < 0)
                                throw new Exception("Error sending packet.");

                            var result = AVCodec.avcodec_receive_frame(stream->codec, frame);
                            if (result == 0)
                            {
                                swsCtx = SWScale.sws_getContext(codecParams.width, codecParams.height, (AVPixelFormat)frame->format, codecParams.width, codecParams.height, AVPixelFormat.AV_PIX_FMT_RGBA, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                                SWScale.sws_scale(swsCtx, &frame->data0, &frame->linesize[0], 0, frame->height, &frameRgb->data0, &frameRgb->linesize[0]);
                                SWScale.sws_freeContext(swsCtx);
                                swsCtx = IntPtr.Zero;

                                var tex = new Texture(codecParams.width, codecParams.height, true);
                                var rawTex = new RawTexture(tex.Width, tex.Height, bufferStack);
                                Marshal.Copy((IntPtr)frameRgb->data0, rawTex.Data, 0, uncompressedFrameSize);
                                tex.SetData(new TextureUpload(rawTex));

                                var frameTime = (frame->best_effort_timestamp - stream->start_time) * timeBaseInSeconds;
                                decodedFrames.Enqueue(new DecodedFrame { Time = frameTime * 1000, Texture = tex });
                                lastDecodedFrameTime = (float)(frameTime * 1000f);
                            }
                        }
                    }

                    while (!decodeFrames || readFrameResult < 0)
                    {
                        if (isDisposed)
                            return;
                        // make sure we process misc commands even while idling
                        var executedCmd = false;
                        while (!decoderCommands.IsEmpty)
                        {
                            if (decoderCommands.TryDequeue(out var cmd))
                            {
                                cmd();
                                executedCmd = true;
                            }
                        }
                        if (executedCmd)
                            break;
                        Thread.Sleep(16);
                        continue;
                    }
                    while (!decoderCommands.IsEmpty)
                    {
                        if (isDisposed)
                            return;
                        if (decoderCommands.TryDequeue(out var cmd))
                            cmd();
                    }
                }
            }
            catch (Exception)
            {
                IsFaulted = true;
            }
            finally
            {
                AVCodec.av_packet_free(&packet);
                SWScale.sws_freeContext(swsCtx);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            while (decoderCommands.TryDequeue(out var cmd)) { }
            if (decodingThread != null)
            {
                isDisposed = true; // decodingThread checks this and aborts if it's set
                decodingThread.Join();
                decodingThread = null;
            }

            if (formatContext != null)
            {
                fixed (AVFormatContext** ptr = &formatContext)
                    AVFormat.avformat_close_input(ptr);
            }

            seekCallback = null;
            readPacketCallback = null;
            managedContextBuffer = null;

            // gets freed by libavformat when closing the input
            contextBuffer = null;

            if (frame != null)
            {
                fixed (AVFrame** ptr = &frame)
                    AVUtil.av_frame_free(ptr);
            }

            if (frameRgb != null)
            {
                fixed (AVFrame** ptr = &frameRgb)
                    AVUtil.av_frame_free(ptr);
            }

            if (frameRgbBufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(frameRgbBufferPtr);
                frameRgbBufferPtr = IntPtr.Zero;
            }

            // do these last to decrease the chance that the decoder writes a single frame into decodedFrames before exiting
            // even if it does happen, it shouldn't be a big deal since the destructor of the created Texture should eventually cause it to be cleaned up
            while (decodedFrames.TryDequeue(out var f))
                f.Texture.Dispose();
            foreach (var f in availableFrames)
                f.Texture.Dispose();
        }
    }
}
