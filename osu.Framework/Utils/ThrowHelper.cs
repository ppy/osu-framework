// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Helper class for throwing exceptions in isolated methods, for cases where method inlining is beneficial.
    /// As throwing directly in that case causes JIT to disable inlining on the surrounding method.
    /// </summary>
    // todo: continue implementation and use where required, see https://github.com/ppy/osu-framework/issues/3470.
    public static class ThrowHelper
    {
        public static void ThrowInvalidOperationException(string? message) => throw new InvalidOperationException(message);
    }
}
