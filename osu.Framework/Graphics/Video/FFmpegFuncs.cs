// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    // ReSharper disable InconsistentNaming
    public unsafe class FFmpegFuncs
    {
        #region Delegates

        public delegate AVFrame* AvFrameAllocDelegate();

        public delegate void AvFrameFreeDelegate(AVFrame** frame);

        public delegate void AvFrameUnrefDelegate(AVFrame* frame);

        public delegate int AvFrameGetBufferDelegate(AVFrame* frame, int align);

        public delegate byte* AvStrDupDelegate(string s);

        public delegate int AvStrErrorDelegate(int errnum, byte* buffer, ulong bufSize);

        public delegate void* AvMallocDelegate(ulong size);

        public delegate AVPacket* AvPacketAllocDelegate();

        public delegate void AvPacketUnrefDelegate(AVPacket* pkt);

        public delegate void AvPacketFreeDelegate(AVPacket** pkt);

        public delegate int AvReadFrameDelegate(AVFormatContext* s, AVPacket* pkt);

        public delegate int AvSeekFrameDelegate(AVFormatContext* s, int stream_index, long timestamp, int flags);

        public delegate AVCodec* AvcodecFindDecoderDelegate(AVCodecID id);

        public delegate int AvcodecOpen2Delegate(AVCodecContext* avctx, AVCodec* codec, AVDictionary** options);

        public delegate int AvcodecReceiveFrameDelegate(AVCodecContext* avctx, AVFrame* frame);

        public delegate int AvcodecSendPacketDelegate(AVCodecContext* avctx, AVPacket* avpkt);

        public delegate AVFormatContext* AvformatAllocContextDelegate();

        public delegate void AvformatCloseInputDelegate(AVFormatContext** s);

        public delegate int AvformatFindStreamInfoDelegate(AVFormatContext* ic, AVDictionary** options);

        public delegate int AvformatOpenInputDelegate(AVFormatContext** ps, [MarshalAs((UnmanagedType)48)] string url, AVInputFormat* fmt, AVDictionary** options);

        public delegate AVIOContext* AvioAllocContextDelegate(byte* buffer, int buffer_size, int write_flag, void* opaque, avio_alloc_context_read_packet_func read_packet, avio_alloc_context_write_packet_func write_packet, avio_alloc_context_seek_func seek);

        public delegate void SwsFreeContextDelegate(SwsContext* swsContext);

        public delegate SwsContext* SwsGetContextDelegate(int srcW, int srcH, AVPixelFormat srcFormat, int dstW, int dstH, AVPixelFormat dstFormat, int flags, SwsFilter* srcFilter, SwsFilter* dstFilter, double* param);

        public delegate int SwsScaleDelegate(SwsContext* c, byte*[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH, byte*[] dst, int[] dstStride);

        #endregion

        public AvFrameAllocDelegate av_frame_alloc;
        public AvFrameFreeDelegate av_frame_free;
        public AvFrameUnrefDelegate av_frame_unref;
        public AvFrameGetBufferDelegate av_frame_get_buffer;
        public AvStrDupDelegate av_strdup;
        public AvStrErrorDelegate av_strerror;
        public AvMallocDelegate av_malloc;
        public AvPacketAllocDelegate av_packet_alloc;
        public AvPacketUnrefDelegate av_packet_unref;
        public AvPacketFreeDelegate av_packet_free;
        public AvReadFrameDelegate av_read_frame;
        public AvSeekFrameDelegate av_seek_frame;
        public AvcodecFindDecoderDelegate avcodec_find_decoder;
        public AvcodecOpen2Delegate avcodec_open2;
        public AvcodecReceiveFrameDelegate avcodec_receive_frame;
        public AvcodecSendPacketDelegate avcodec_send_packet;
        public AvformatAllocContextDelegate avformat_alloc_context;
        public AvformatCloseInputDelegate avformat_close_input;
        public AvformatFindStreamInfoDelegate avformat_find_stream_info;
        public AvformatOpenInputDelegate avformat_open_input;
        public AvioAllocContextDelegate avio_alloc_context;
        public SwsFreeContextDelegate sws_freeContext;
        public SwsGetContextDelegate sws_getContext;
        public SwsScaleDelegate sws_scale;
    }
}
