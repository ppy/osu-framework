// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Development
{
    internal static class ThreadSafety
    {
        [Conditional("DEBUG")]
        internal static void EnsureUpdateThread() => Debug.Assert(IsUpdateThread);

        [Conditional("DEBUG")]
        internal static void EnsureNotUpdateThread() => Debug.Assert(!IsUpdateThread);

        [Conditional("DEBUG")]
        internal static void EnsureDrawThread() => Debug.Assert(IsDrawThread);

        [ThreadStatic]
        public static bool IsInputThread;

        [ThreadStatic]
        public static bool IsUpdateThread;

        [ThreadStatic]
        public static bool IsDrawThread;

        [ThreadStatic]
        public static bool IsAudioThread;
    }
}
