// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    // ReSharper disable once InconsistentNaming
    internal class AVHWDeviceTypePerformanceComparer : Comparer<AVHWDeviceType>
    {
        // higher = better
        private static readonly IReadOnlyDictionary<AVHWDeviceType, int> performance_scores = new Dictionary<AVHWDeviceType, int>
        {
            // Windows
            { AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA, 10 },
            { AVHWDeviceType.AV_HWDEVICE_TYPE_QSV, 9 },
            { AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2, 8 },
            // Linux
            { AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU, 10 },
            { AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI, 9 },
            // Android
            { AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC, 10 },
            // iOS, macOS
            { AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX, 10 },
        };

        public override int Compare(AVHWDeviceType x, AVHWDeviceType y)
        {
            int xScore = performance_scores.GetValueOrDefault(x, int.MinValue);
            int yScore = performance_scores.GetValueOrDefault(y, int.MinValue);

            return -Comparer<int>.Default.Compare(xScore, yScore);
        }
    }
}
