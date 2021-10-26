// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    internal static class FfmpegExtensions
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
    }
}
