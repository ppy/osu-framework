//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using System.Threading;
using osu.Framework.OS;

namespace osu.Framework.DebugUtils
{
    internal static class ThreadSafety
    {
        [Conditional("DEBUG")]
        internal static void EnsureUpdateThread()
        {
            //This check is very intrusive on performance, so let's only run when a debugger is actually attached.
            if (!Debugger.IsAttached) return;

            Debug.Assert(Thread.CurrentThread == BasicGameHost.UpdateThread);
        }

        [Conditional("DEBUG")]
        internal static void EnsureDrawThread()
        {
            //This check is very intrusive on performance, so let's only run when a debugger is actually attached.
            if (!Debugger.IsAttached) return;

            Debug.Assert(Thread.CurrentThread == BasicGameHost.DrawThread);
        }
    }
}
