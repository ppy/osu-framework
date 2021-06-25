// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;

namespace osu.Framework.Threading
{
    public enum GameThreadState
    {
        /// <summary>
        /// This thread has not yet been started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// This thread is preparing to run.
        /// </summary>
        Starting,

        /// <summary>
        /// This thread is running.
        /// </summary>
        Running,

        /// <summary>
        /// This thread is paused to be moved to a different native thread. This occurs when <see cref="ExecutionMode"/> changes.
        /// </summary>
        Paused,

        /// <summary>
        /// This thread has permanently exited.
        /// </summary>
        Exited
    }
}
