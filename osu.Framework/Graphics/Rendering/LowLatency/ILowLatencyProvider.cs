// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.LowLatency
{
    public interface ILowLatencyProvider
    {
        bool IsAvailable { get; }

        void Initialize(IntPtr nativeDeviceHandle);

        void SetMode(LatencyMode mode);

        void SetMarker(LatencyMarker marker, ulong frameId);

        void FrameSleep();
    }
}
