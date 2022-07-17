// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Platform;

namespace osu.Framework.Development
{
    /// <summary>
    /// Utilities to ensure correct thread usage throughout a game.
    /// </summary>
    public static class ThreadSafety
    {
        /// <summary>
        /// Whether the current code is executing on the input thread.
        /// </summary>
        [field: ThreadStatic]
        public static bool IsInputThread { get; internal set; }

        /// <summary>
        /// Whether the current code is executing on the update thread.
        /// </summary>
        [field: ThreadStatic]
        public static bool IsUpdateThread { get; internal set; }

        /// <summary>
        /// Whether the current code is executing on the draw thread.
        /// </summary>
        [field: ThreadStatic]
        public static bool IsDrawThread { get; internal set; }

        /// <summary>
        /// Whether the current code is executing on the audio thread.
        /// </summary>
        [field: ThreadStatic]
        public static bool IsAudioThread { get; internal set; }

        /// <summary>
        /// Asserts that the current code is executing on the input thread.
        /// </summary>
        /// <remarks>
        /// Only asserts in debug builds due to performance concerns.
        /// </remarks>
        [Conditional("DEBUG")]
        internal static void EnsureInputThread() => Debug.Assert(IsInputThread);

        /// <summary>
        /// Asserts that the current code is executing on the update thread.
        /// </summary>
        /// <remarks>
        /// Only asserts in debug builds due to performance concerns.
        /// </remarks>
        [Conditional("DEBUG")]
        internal static void EnsureUpdateThread() => Debug.Assert(IsUpdateThread);

        /// <summary>
        /// Asserts that the current code is not executing on the update thread.
        /// </summary>
        /// <remarks>
        /// Only asserts in debug builds due to performance concerns.
        /// </remarks>
        [Conditional("DEBUG")]
        internal static void EnsureNotUpdateThread() => Debug.Assert(!IsUpdateThread);

        /// <summary>
        /// Asserts that the current code is executing on the draw thread.
        /// </summary>
        /// <remarks>
        /// Only asserts in debug builds due to performance concerns.
        /// </remarks>
        [Conditional("DEBUG")]
        internal static void EnsureDrawThread() => Debug.Assert(IsDrawThread);

        /// <summary>
        /// Asserts that the current code is executing on the audio thread.
        /// </summary>
        /// <remarks>
        /// Only asserts in debug builds due to performance concerns.
        /// </remarks>
        [Conditional("DEBUG")]
        internal static void EnsureAudioThread() => Debug.Assert(IsAudioThread);

        /// <summary>
        /// The current execution mode.
        /// </summary>
        internal static ExecutionMode ExecutionMode;

        /// <summary>
        /// Resets all statics for the current thread.
        /// </summary>
        internal static void ResetAllForCurrentThread()
        {
            IsInputThread = false;
            IsUpdateThread = false;
            IsDrawThread = false;
            IsAudioThread = false;
        }
    }
}
