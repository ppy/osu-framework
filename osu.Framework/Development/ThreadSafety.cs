// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Platform;

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

        internal static void ResetAllForCurrentThread()
        {
            IsInputThread = false;
            IsUpdateThread = false;
            IsDrawThread = false;
            IsAudioThread = false;
        }

        public static ExecutionMode ExecutionMode;

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
