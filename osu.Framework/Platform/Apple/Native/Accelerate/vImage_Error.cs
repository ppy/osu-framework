// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform.Apple.Native.Accelerate
{
    internal enum vImage_Error : long
    {
        OutOfPlaceOperationRequired = -21780,
        ColorSyncIsAbsent = -21779,
        InvalidImageFormat = -21778,
        InvalidRowBytes = -21777,
        InternalError = -21776,
        UnknownFlagsBit = -21775,
        BufferSizeMismatch = -21774,
        InvalidParameter = -21773,
        NullPointerArgument = -21772,
        MemoryAllocationError = -21771,
        InvalidOffsetY = -21770,
        InvalidOffsetX = -21769,
        InvalidEdgeStyle = -21768,
        InvalidKernelSize = -21767,
        RoiLargerThanInputBuffer = -21766,
        NoError = 0,
    }
}
