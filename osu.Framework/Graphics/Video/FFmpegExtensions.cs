// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    internal static class FFmpegExtensions
    {
        internal static double GetValue(this AVRational rational) => rational.num / (double)rational.den;

        internal static bool IsHardwarePixelFormat(this AVPixelFormat pixFmt)
        {
            switch (pixFmt)
            {
                case AVPixelFormat.AV_PIX_FMT_VDPAU:
                case AVPixelFormat.AV_PIX_FMT_CUDA:
                case AVPixelFormat.AV_PIX_FMT_VAAPI:
                case AVPixelFormat.AV_PIX_FMT_VAAPI_IDCT:
                case AVPixelFormat.AV_PIX_FMT_VAAPI_MOCO:
                case AVPixelFormat.AV_PIX_FMT_DXVA2_VLD:
                case AVPixelFormat.AV_PIX_FMT_QSV:
                case AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX:
                case AVPixelFormat.AV_PIX_FMT_D3D11:
                case AVPixelFormat.AV_PIX_FMT_D3D11VA_VLD:
                case AVPixelFormat.AV_PIX_FMT_DRM_PRIME:
                case AVPixelFormat.AV_PIX_FMT_OPENCL:
                case AVPixelFormat.AV_PIX_FMT_MEDIACODEC:
                case AVPixelFormat.AV_PIX_FMT_VULKAN:
                case AVPixelFormat.AV_PIX_FMT_MMAL:
                case AVPixelFormat.AV_PIX_FMT_XVMC:
                    return true;

                default:
                    return false;
            }
        }

        public static HardwareVideoDecoder? ToHardwareVideoDecoder(this AVHWDeviceType hwDeviceType)
        {
            switch (hwDeviceType)
            {
                case AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA:
                    return HardwareVideoDecoder.NVDEC;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_QSV:
                    return HardwareVideoDecoder.QuickSyncVideo;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2:
                    return HardwareVideoDecoder.DXVA2;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU:
                    return HardwareVideoDecoder.VDPAU;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI:
                    return HardwareVideoDecoder.VAAPI;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC:
                    return HardwareVideoDecoder.MediaCodec;

                case AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX:
                    return HardwareVideoDecoder.VideoToolbox;

                default:
                    return null;
            }
        }
    }
}
