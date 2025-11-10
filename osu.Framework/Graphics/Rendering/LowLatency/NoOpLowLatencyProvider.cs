// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.LowLatency
{
    internal sealed class NoOpLowLatencyProvider : ILowLatencyProvider
    {
        public static readonly NoOpLowLatencyProvider INSTANCE = new NoOpLowLatencyProvider();

        public bool IsAvailable => false;

        public void Initialize(IntPtr deviceHandle) { }

        public void SetMode(LatencyMode mode) { }

        public void SetMarker(LatencyMarker marker, ulong frameId) { }

        public void FrameSleep() { }
    }
}
