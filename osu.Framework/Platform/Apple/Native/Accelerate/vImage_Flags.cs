// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    internal enum vImage_Flags : uint
    {
        NoFlags = 0,
        LeaveAlphaUnchanged = 1,
        CopyInPlace = 2,
        BackgroundColorFill = 4,
        EdgeExtend = 8,
        DoNotTile = 16,
        HighQualityResampling = 32,
        TruncateKernel = 64,
        GetTempBufferSize = 128,
        PrintDiagnosticsToConsole = 256,
        NoAllocate = 512,
    }
}
