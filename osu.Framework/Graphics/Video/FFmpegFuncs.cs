// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming style

namespace osu.Framework.Graphics.Video
{
    public unsafe class FFmpegFuncs
    {
        #region Delegates

        public delegate AVFrame* AvFrameAllocDelegate();

        public delegate void AvFrameFreeDelegate(AVFrame** frame);

        public delegate void AvFrameUnrefDelegate(AVFrame* frame);

        public delegate void AvFrameMoveRefDelegate(AVFrame* dst, AVFrame* src);

        public delegate int AvFrameGetBufferDelegate(AVFrame* frame, int align);

        public delegate byte* AvStrDupDelegate(string s);

        public delegate int AvStrErrorDelegate(int errnum, byte* buffer, ulong bufSize);

        public delegate void* AvMallocDelegate(ulong size);

        public delegate void AvFreepDelegate(void* ptr);

        public delegate AVPacket* AvPacketAllocDelegate();

        public delegate void AvPacketUnrefDelegate(AVPacket* pkt);

        public delegate void AvPacketFreeDelegate(AVPacket** pkt);

        public delegate int AvReadFrameDelegate(AVFormatContext* s, AVPacket* pkt);

        public delegate int AvSeekFrameDelegate(AVFormatContext* s, int stream_index, long timestamp, int flags);

        public delegate int AvHwdeviceCtxCreateDelegate(AVBufferRef** device_ctx, AVHWDeviceType type, [MarshalAs(UnmanagedType.LPUTF8Str)] string device, AVDictionary* opts, int flags);

        public delegate int AvHwframeTransferDataDelegate(AVFrame* dst, AVFrame* src, int flags);

        public delegate AVCodec* AvCodecIterateDelegate(void** opaque);

        public delegate int AvCodecIsDecoderDelegate(AVCodec* codec);

        public delegate AVCodecHWConfig* AvcodecGetHwConfigDelegate(AVCodec* codec, int index);

        public delegate AVCodecContext* AvcodecAllocContext3Delegate(AVCodec* codec);

        public delegate void AvcodecFreeContextDelegate(AVCodecContext** avctx);

        public delegate int AvcodecParametersToContextDelegate(AVCodecContext* codec, AVCodecParameters* par);

        public delegate int AvcodecOpen2Delegate(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        public delegate int AvcodecReceiveFrameDelegate(AVCodecContext* avctx, AVFrame* frame);

        public delegate int AvcodecSendPacketDelegate(AVCodecContext* avctx, AVPacket* avpkt);

        public delegate void AvcodecFlushBuffersDelegate(AVCodecContext* avctx);

        public delegate AVFormatContext* AvformatAllocContextDelegate();

        public delegate void AvformatCloseInputDelegate(AVFormatContext** s);

        public delegate int AvformatFindStreamInfoDelegate(AVFormatContext* ic, AVDictionary** options);

        public delegate int AvformatOpenInputDelegate(AVFormatContext** ps, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, AVInputFormat* fmt, AVDictionary** options);

        public delegate int AvFindBestStreamDelegate(AVFormatContext* ic, AVMediaType type, int wanted_stream_nb, int related_stream, AVCodec** decoder_ret, int flags);

        public delegate AVIOContext* AvioAllocContextDelegate(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        public delegate void AvioContextFreeDelegate(AVIOContext** s);

        public delegate void SwsFreeContextDelegate(SwsContext* swsContext);

        public delegate SwsContext* SwsGetCachedContextDelegate(SwsContext* context, int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, SwsFilter* srcFilter, SwsFilter* dstFilter, double* param);

        public delegate int SwsScaleDelegate(SwsContext* c, byte*[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH, byte*[] dst, int[] dstStride);

        #endregion

        public AvFrameAllocDelegate av_frame_alloc;
        public AvFrameFreeDelegate av_frame_free;
        public AvFrameUnrefDelegate av_frame_unref;
        public AvFrameMoveRefDelegate av_frame_move_ref;
        public AvFrameGetBufferDelegate av_frame_get_buffer;
        public AvStrDupDelegate av_strdup;
        public AvStrErrorDelegate av_strerror;
        public AvMallocDelegate av_malloc;
        public AvFreepDelegate av_freep;
        public AvPacketAllocDelegate av_packet_alloc;
        public AvPacketUnrefDelegate av_packet_unref;
        public AvPacketFreeDelegate av_packet_free;
        public AvReadFrameDelegate av_read_frame;
        public AvSeekFrameDelegate av_seek_frame;
        public AvHwdeviceCtxCreateDelegate av_hwdevice_ctx_create;
        public AvHwframeTransferDataDelegate av_hwframe_transfer_data;
        public AvCodecIterateDelegate av_codec_iterate;
        public AvCodecIsDecoderDelegate av_codec_is_decoder;
        public AvcodecGetHwConfigDelegate avcodec_get_hw_config;
        public AvcodecAllocContext3Delegate avcodec_alloc_context3;
        public AvcodecFreeContextDelegate avcodec_free_context;
        public AvcodecParametersToContextDelegate avcodec_parameters_to_context;
        public AvcodecOpen2Delegate avcodec_open2;
        public AvcodecReceiveFrameDelegate avcodec_receive_frame;
        public AvcodecSendPacketDelegate avcodec_send_packet;
        public AvcodecFlushBuffersDelegate avcodec_flush_buffers;
        public AvformatAllocContextDelegate avformat_alloc_context;
        public AvformatCloseInputDelegate avformat_close_input;
        public AvformatFindStreamInfoDelegate avformat_find_stream_info;
        public AvformatOpenInputDelegate avformat_open_input;
        public AvFindBestStreamDelegate av_find_best_stream;
        public AvioAllocContextDelegate avio_alloc_context;
        public AvioContextFreeDelegate avio_context_free;
        public SwsFreeContextDelegate sws_freeContext;
        public SwsGetCachedContextDelegate sws_getCachedContext;
        public SwsScaleDelegate sws_scale;
    }
}
