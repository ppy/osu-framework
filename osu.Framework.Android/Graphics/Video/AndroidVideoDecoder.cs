// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.Android.Graphics.Video
{
    // ReSharper disable InconsistentNaming
    public unsafe class AndroidVideoDecoder : VideoDecoder
    {
        private const string lib_avutil = "libavutil.so";
        private const string lib_avcodec = "libavcodec.so";
        private const string lib_avformat = "libavformat.so";
        private const string lib_swscale = "libswscale.so";

        [DllImport(lib_avutil)]
        public static extern AVFrame* av_frame_alloc();

        [DllImport(lib_avutil)]
        public static extern void av_frame_free(AVFrame** frame);

        [DllImport(lib_avutil)]
        public static extern int av_image_fill_arrays(ref byte_ptrArray4 dst_data, ref int_array4 dst_linesize, byte* src, AVPixelFormat pix_fmt, int width, int height, int align);

        [DllImport(lib_avutil)]
        public static extern int av_image_get_buffer_size(AVPixelFormat pix_fmt, int width, int height, int align);

        [DllImport(lib_avutil)]
        public static extern void* av_malloc(ulong size);

        [DllImport(lib_avcodec)]
        public static extern AVPacket* av_packet_alloc();

        [DllImport(lib_avcodec)]
        public static extern void av_packet_free(AVPacket** pkt);

        [DllImport(lib_avformat)]
        public static extern int av_read_frame(AVFormatContext* s, AVPacket* pkt);

        [DllImport(lib_avformat)]
        public static extern int av_seek_frame(AVFormatContext* s, int stream_index, long timestamp, int flags);

        [DllImport(lib_avcodec)]
        public static extern AVCodec* avcodec_find_decoder(AVCodecID id);

        [DllImport(lib_avcodec)]
        public static extern int avcodec_open2(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        [DllImport(lib_avcodec)]
        public static extern int avcodec_receive_frame(AVCodecContext* avctx, AVFrame* frame);

        [DllImport(lib_avcodec)]
        public static extern int avcodec_send_packet(AVCodecContext* avctx, AVPacket* avpkt);

        [DllImport(lib_avformat)]
        public static extern AVFormatContext* avformat_alloc_context();

        [DllImport(lib_avformat)]
        public static extern void avformat_close_input(AVFormatContext** s);

        [DllImport(lib_avformat)]
        public static extern int avformat_find_stream_info(AVFormatContext* ic, AVDictionary** options);

        [DllImport(lib_avformat)]
        public static extern int avformat_open_input(AVFormatContext** ps, [MarshalAs((UnmanagedType)48)] string url, AVInputFormat* fmt, AVDictionary** options);

        [DllImport(lib_avformat)]
        public static extern AVIOContext* avio_alloc_context(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        [DllImport(lib_swscale)]
        public static extern void sws_freeContext(SwsContext* swsContext);

        [DllImport(lib_swscale)]
        public static extern SwsContext* sws_getContext(int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, SwsFilter* srcFilter, SwsFilter* dstFilter, double* param);

        [DllImport(lib_swscale)]
        public static extern int sws_scale(SwsContext* c, byte*[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH, byte*[] dst, int[] dstStride);

        public AndroidVideoDecoder(string filename, Scheduler scheduler)
            : base(filename, scheduler)
        {
        }

        public AndroidVideoDecoder(Stream videoStream, Scheduler scheduler)
            : base(videoStream, scheduler)
        {
        }

        protected override FFmpegFuncs CreateFuncs() => new FFmpegFuncs
        {
            av_frame_alloc = av_frame_alloc,
            av_frame_free = av_frame_free,
            av_image_fill_arrays = av_image_fill_arrays,
            av_image_get_buffer_size = av_image_get_buffer_size,
            av_malloc = av_malloc,
            av_packet_alloc = av_packet_alloc,
            av_packet_free = av_packet_free,
            av_read_frame = av_read_frame,
            av_seek_frame = av_seek_frame,
            avcodec_find_decoder = avcodec_find_decoder,
            avcodec_open2 = avcodec_open2,
            avcodec_receive_frame = avcodec_receive_frame,
            avcodec_send_packet = avcodec_send_packet,
            avformat_alloc_context = avformat_alloc_context,
            avformat_close_input = avformat_close_input,
            avformat_find_stream_info = avformat_find_stream_info,
            avformat_open_input = avformat_open_input,
            avio_alloc_context = avio_alloc_context,
            sws_freeContext = sws_freeContext,
            sws_getContext = sws_getContext,
            sws_scale = sws_scale
        };
    }
}
