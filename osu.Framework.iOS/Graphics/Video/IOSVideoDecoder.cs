// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Video;

namespace osu.Framework.iOS.Graphics.Video
{
    // ReSharper disable InconsistentNaming
    public unsafe class IOSVideoDecoder : VideoDecoder
    {
        private const string dll_name = "__Internal";

        [DllImport(dll_name)]
        private static extern AVFrame* av_frame_alloc();

        [DllImport(dll_name)]
        private static extern void av_frame_free(AVFrame** frame);

        [DllImport(dll_name)]
        private static extern void av_frame_unref(AVFrame* frame);

        [DllImport(dll_name)]
        private static extern int av_frame_get_buffer(AVFrame* frame, int align);

        [DllImport(dll_name)]
        private static extern byte* av_strdup(string s);

        [DllImport(dll_name)]
        private static extern int av_strerror(int errnum, byte* buffer, ulong bufSize);

        [DllImport(dll_name)]
        private static extern void* av_malloc(ulong size);

        [DllImport(dll_name)]
        private static extern void av_freep(void* ptr);

        [DllImport(dll_name)]
        private static extern AVPacket* av_packet_alloc();

        [DllImport(dll_name)]
        private static extern void av_packet_unref(AVPacket* pkt);

        [DllImport(dll_name)]
        private static extern void av_frame_move_ref(AVFrame* dst, AVFrame* src);

        [DllImport(dll_name)]
        private static extern void av_packet_free(AVPacket** pkt);

        [DllImport(dll_name)]
        private static extern int av_read_frame(AVFormatContext* s, AVPacket* pkt);

        [DllImport(dll_name)]
        private static extern int av_seek_frame(AVFormatContext* s, int stream_index, long timestamp, int flags);

        [DllImport(dll_name)]
        private static extern int av_hwdevice_ctx_create(AVBufferRef** device_ctx, AVHWDeviceType type, [MarshalAs(UnmanagedType.LPUTF8Str)] string device, AVDictionary* opts, int flags);

        [DllImport(dll_name)]
        private static extern int av_hwframe_transfer_data(AVFrame* dst, AVFrame* src, int flags);

        [DllImport(dll_name)]
        private static extern AVCodec* av_codec_iterate(void** opaque);

        [DllImport(dll_name)]
        private static extern int av_codec_is_decoder(AVCodec* codec);

        [DllImport(dll_name)]
        private static extern AVCodecHWConfig* avcodec_get_hw_config(AVCodec* codec, int index);

        [DllImport(dll_name)]
        private static extern AVCodecContext* avcodec_alloc_context3(AVCodec* codec);

        [DllImport(dll_name)]
        private static extern void avcodec_free_context(AVCodecContext** avctx);

        [DllImport(dll_name)]
        private static extern int avcodec_parameters_to_context(AVCodecContext* codec, AVCodecParameters* par);

        [DllImport(dll_name)]
        private static extern int avcodec_open2(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern int avcodec_receive_frame(AVCodecContext* avctx, AVFrame* frame);

        [DllImport(dll_name)]
        private static extern int avcodec_send_packet(AVCodecContext* avctx, AVPacket* avpkt);

        [DllImport(dll_name)]
        private static extern void avcodec_flush_buffers(AVCodecContext* avctx);

        [DllImport(dll_name)]
        private static extern AVFormatContext* avformat_alloc_context();

        [DllImport(dll_name)]
        private static extern void avformat_close_input(AVFormatContext** s);

        [DllImport(dll_name)]
        private static extern int avformat_find_stream_info(AVFormatContext* ic, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern int avformat_open_input(AVFormatContext** ps, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, AVInputFormat* fmt, AVDictionary** options);

        [DllImport(dll_name)]
        private static extern int av_find_best_stream(AVFormatContext* ic, AVMediaType type, int wanted_stream_nb, int related_stream, AVCodec** decoder_ret, int flags);

        [DllImport(dll_name)]
        private static extern AVIOContext* avio_alloc_context(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        [DllImport(dll_name)]
        private static extern void avio_context_free(AVIOContext** s);

        [DllImport(dll_name)]
        private static extern void sws_freeContext(SwsContext* swsContext);

        [DllImport(dll_name)]
        private static extern SwsContext* sws_getCachedContext(SwsContext* context, int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, SwsFilter* srcFilter, SwsFilter* dstFilter, double* param);

        [DllImport(dll_name)]
        private static extern int sws_scale(SwsContext* c, byte*[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH, byte*[] dst, int[] dstStride);

        public IOSVideoDecoder(IRenderer renderer, string filename)
            : base(renderer, filename)
        {
        }

        public IOSVideoDecoder(IRenderer renderer, Stream videoStream)
            : base(renderer, videoStream)
        {
        }

        protected override FFmpegFuncs CreateFuncs() => new FFmpegFuncs
        {
            av_frame_alloc = av_frame_alloc,
            av_frame_free = av_frame_free,
            av_frame_unref = av_frame_unref,
            av_frame_move_ref = av_frame_move_ref,
            av_frame_get_buffer = av_frame_get_buffer,
            av_strdup = av_strdup,
            av_strerror = av_strerror,
            av_malloc = av_malloc,
            av_freep = av_freep,
            av_packet_alloc = av_packet_alloc,
            av_packet_unref = av_packet_unref,
            av_packet_free = av_packet_free,
            av_read_frame = av_read_frame,
            av_seek_frame = av_seek_frame,
            av_hwdevice_ctx_create = av_hwdevice_ctx_create,
            av_hwframe_transfer_data = av_hwframe_transfer_data,
            av_codec_iterate = av_codec_iterate,
            av_codec_is_decoder = av_codec_is_decoder,
            avcodec_get_hw_config = avcodec_get_hw_config,
            avcodec_alloc_context3 = avcodec_alloc_context3,
            avcodec_free_context = avcodec_free_context,
            avcodec_parameters_to_context = avcodec_parameters_to_context,
            avcodec_open2 = avcodec_open2,
            avcodec_receive_frame = avcodec_receive_frame,
            avcodec_send_packet = avcodec_send_packet,
            avcodec_flush_buffers = avcodec_flush_buffers,
            avformat_alloc_context = avformat_alloc_context,
            avformat_close_input = avformat_close_input,
            avformat_find_stream_info = avformat_find_stream_info,
            avformat_open_input = avformat_open_input,
            av_find_best_stream = av_find_best_stream,
            avio_alloc_context = avio_alloc_context,
            avio_context_free = avio_context_free,
            sws_freeContext = sws_freeContext,
            sws_getCachedContext = sws_getCachedContext,
            sws_scale = sws_scale
        };
    }
}
