// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
            //This check is very intrusive on performance, so let's only run when a debugger is actually attached.
            if (!Debugger.IsAttached) return;

            Debug.Assert(IsUpdateThread);
        }

        [Conditional("DEBUG")]
        internal static void EnsureNotUpdateThread()
        {
            //This check is very intrusive on performance, so let's only run when a debugger is actually attached.
            if (!Debugger.IsAttached) return;

            Debug.Assert(!IsUpdateThread);
        }

        [Conditional("DEBUG")]
        internal static void EnsureDrawThread()
        {
            //This check is very intrusive on performance, so let's only run when a debugger is actually attached.
            if (!Debugger.IsAttached) return;

            Debug.Assert(IsDrawThread);
        }

        public static bool IsUpdateThread => Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Update");

        public static bool IsDrawThread => Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Draw");

        public static bool IsAudioThread => Thread.CurrentThread.Name == GameThread.PrefixedThreadNameFor("Audio");
    }
}
