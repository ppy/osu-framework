// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;

namespace osu.Framework.Utils
{
    public static class ThrowHelper
    {
        public static void ThrowInvalidOperationException(string? message) => throw new InvalidOperationException(message);
    }
}
