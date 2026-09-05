// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.LowLatency
{
    internal sealed class NoOpDirect3D11LowLatencyProvider : IDirect3D11LowLatencyProvider
    {
        public static readonly NoOpDirect3D11LowLatencyProvider INSTANCE = new NoOpDirect3D11LowLatencyProvider();

        public bool IsAvailable => false;

        public void Initialize(IntPtr deviceHandle) { }

        public void SetMode(LatencyMode mode) { }

        public void SetMarker(LatencyMarker marker, ulong frameId) { }

        public void FrameSleep() { }
    }
}
