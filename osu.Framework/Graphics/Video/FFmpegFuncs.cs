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

        public delegate byte* AvStrDupDelegate(string s);

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

        public delegate AVFilter* AvfilterGetFilterByNameDelegate(string name);

        public delegate AVFilterInOut* AvfilterInOutAllocDelegate();

        public delegate int AvfilterGraphCreateFilterDelegate(AVFilterContext** filt_ctx, AVFilter* filt, string name, string args, void* opaque, AVFilterGraph* graph_ctx);

        public delegate AVFilterGraph* AvfilterGraphAlllocDelegate();

        public delegate void AvfilterGraphFreeDelegate(AVFilterGraph** graph);

        public delegate int AvfilterGraphParsePtrDelegate(AVFilterGraph* graph, string filters, AVFilterInOut** inputs, AVFilterInOut** outputs, void* log_ctx);

        public delegate int AvfilterGraphConfigDelegate(AVFilterGraph* graphctx, void* log_ctx);

        public delegate int AvbufferSrcAddFrameFlagsDelegate(AVFilterContext* buffer_src, AVFrame* frame, int flags);

        public delegate int AvbufferSinkGetFrameDelegate(AVFilterContext* ctx, AVFrame* frame);

        #endregion

        public AvFrameAllocDelegate av_frame_alloc;
        public AvFrameFreeDelegate av_frame_free;
        public AvFrameUnrefDelegate av_frame_unref;
        public AvStrDupDelegate av_strdup;
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
        public AvfilterGetFilterByNameDelegate avfilter_get_by_name;
        public AvfilterInOutAllocDelegate avfilter_inout_alloc;
        public AvfilterGraphCreateFilterDelegate avfilter_graph_create_filter;
        public AvfilterGraphParsePtrDelegate avfilter_graph_parse_ptr;
        public AvfilterGraphConfigDelegate avfilter_graph_config;
        public AvfilterGraphAlllocDelegate avfilter_graph_alloc;
        public AvfilterGraphFreeDelegate avfilter_graph_free;
        public AvbufferSrcAddFrameFlagsDelegate av_buffersrc_add_frame_flags;
        public AvbufferSinkGetFrameDelegate av_buffersink_get_frame;
    }
}
