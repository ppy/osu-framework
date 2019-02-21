// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Threading;
using osu.Framework.Threading;

namespace osu.Framework.Development
{
    internal static class ThreadSafety
    {
        [Conditional("DEBUG")]
        internal static void EnsureUpdateThread()
        {
            Debug.Assert(IsUpdateThread);
        }

        [Conditional("DEBUG")]
        internal static void EnsureNotUpdateThread()
        {
            Debug.Assert(!IsUpdateThread);
        }

        [Conditional("DEBUG")]
        internal static void EnsureDrawThread()
        {
            Debug.Assert(IsDrawThread);
        }

        private static readonly ThreadLocal<bool> is_update_thread = new ThreadLocal<bool>(() =>
            Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Update"));

        private static readonly ThreadLocal<bool> is_draw_thread = new ThreadLocal<bool>(() =>
            Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Draw"));

        private static readonly ThreadLocal<bool> is_audio_thread = new ThreadLocal<bool>(() =>
            Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Audio"));

        public static bool IsUpdateThread => is_update_thread.Value;

        public static bool IsDrawThread => is_draw_thread.Value;

        public static bool IsAudioThread => is_audio_thread.Value;
    }
}
