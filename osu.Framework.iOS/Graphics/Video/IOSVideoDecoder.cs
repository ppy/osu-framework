// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.iOS.Graphics.Video
{
    // ReSharper disable InconsistentNaming
    public unsafe class IOSVideoDecoder : VideoDecoder
    {
        private const string dll_name = "__Internal";

        [DllImport(dll_name)]
        public static extern AVFrame* av_frame_alloc();

        [DllImport(dll_name)]
        public static extern void av_frame_free(AVFrame** frame);

        [DllImport(dll_name)]
        public static extern int av_image_fill_arrays(ref byte_ptrArray4 dst_data, ref int_array4 dst_linesize, byte* src, AVPixelFormat pix_fmt, int width, int height, int align);

        [DllImport(dll_name)]
        public static extern int av_image_get_buffer_size(AVPixelFormat pix_fmt, int width, int height, int align);

        [DllImport(dll_name)]
        public static extern void* av_malloc(ulong size);

        [DllImport(dll_name)]
        public static extern AVPacket* av_packet_alloc();

        [DllImport(dll_name)]
        public static extern void av_packet_free(AVPacket** pkt);

        [DllImport(dll_name)]
        public static extern int av_read_frame(AVFormatContext* s, AVPacket* pkt);

        [DllImport(dll_name)]
        public static extern int av_seek_frame(AVFormatContext* s, int stream_index, long timestamp, int flags);

        [DllImport(dll_name)]
        public static extern AVCodec* avcodec_find_decoder(AVCodecID id);

        [DllImport(dll_name)]
        public static extern int avcodec_open2(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        [DllImport(dll_name)]
        public static extern int avcodec_receive_frame(AVCodecContext* avctx, AVFrame* frame);

        [DllImport(dll_name)]
        public static extern int avcodec_send_packet(AVCodecContext* avctx, AVPacket* avpkt);

        [DllImport(dll_name)]
        public static extern AVFormatContext* avformat_alloc_context();

        [DllImport(dll_name)]
        public static extern void avformat_close_input(AVFormatContext** s);

        [DllImport(dll_name)]
        public static extern int avformat_find_stream_info(AVFormatContext* ic, AVDictionary** options);

        [DllImport(dll_name)]
        public static extern int avformat_open_input(AVFormatContext** ps, [MarshalAs((UnmanagedType)48)] string url, AVInputFormat* fmt, AVDictionary** options);

        [DllImport(dll_name)]
        public static extern AVIOContext* avio_alloc_context(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        [DllImport(dll_name)]
        public static extern void sws_freeContext(SwsContext* swsContext);

        [DllImport(dll_name)]
        public static extern SwsContext* sws_getContext(int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, SwsFilter* srcFilter, SwsFilter* dstFilter, double* param);

        [DllImport(dll_name)]
        public static extern int sws_scale(SwsContext* c, byte*[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH, byte*[] dst, int[] dstStride);

        public IOSVideoDecoder(string filename, Scheduler scheduler)
            : base(filename, scheduler)
        {
        }

        public IOSVideoDecoder(Stream videoStream, Scheduler scheduler)
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
