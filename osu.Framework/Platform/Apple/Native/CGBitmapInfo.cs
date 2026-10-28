// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.Apple.Native
{
    public enum CGBitmapInfo : uint
    {
        None,
        PremultipliedLast,
        PremultipliedFirst,
        Last,
        First,
        NoneSkipLast,
        NoneSkipFirst,
        Only,
        AlphaInfoMask = 31,
        FloatInfoMask = 3840,
        FloatComponents = 256,
        ByteOrderMask = 28672,
        ByteOrderDefault = 0,
        ByteOrder16Little = 4096,
        ByteOrder32Little = 8192,
        ByteOrder16Big = 12288,
        ByteOrder32Big = 16384,
    }
}
