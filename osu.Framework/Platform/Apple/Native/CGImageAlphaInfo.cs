// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.Apple.Native
{
    internal enum CGImageAlphaInfo : uint
    {
        None,
        PremultipliedLast,
        PremultipliedFirst,
        Last,
        First,
        NoneSkipLast,
        NoneSkipFirst,
        Only,
    }
}
