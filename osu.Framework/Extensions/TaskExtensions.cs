// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Safe alternative to Task.Wait which ensures the calling thread is not a thread pool thread.
        /// </summary>
        public static void WaitSafely(this Task task)
        {
            if (!isWaitingValid(task))
                throw new InvalidOperationException($"Can't use {nameof(WaitSafely)} from inside an async operation.");

#pragma warning disable RS0030
            task.Wait();
#pragma warning restore RS0030
        }

        /// <summary>
        /// Safe alternative to Task.Result which ensures the calling thread is not a thread pool thread.
        /// </summary>
        public static T GetResultSafely<T>(this Task<T> task)
        {
            // We commonly access `.Result` from within `ContinueWith`, which is a safe usage (the task is guaranteed to be completed).
            // Unfortunately, the only way to allow these usages is to check whether the task is completed or not here.
            // This does mean that there could be edge cases where this safety is skipped (ie. if the majority of executions complete
            // immediately).
            if (!task.IsCompleted && !isWaitingValid(task))
                throw new InvalidOperationException($"Can't use {nameof(GetResultSafely)} from inside an async operation.");

#pragma warning disable RS0030
            return task.Result;
#pragma warning restore RS0030
        }

        private static bool isWaitingValid(Task task)
        {
            // In the case the task has been started with the LongRunning flag, it will not be in the TPL thread pool and we can allow waiting regardless.
            if (task.CreationOptions.HasFlagFast(TaskCreationOptions.LongRunning))
                return true;

            // Otherwise only allow waiting from a non-TPL thread pool thread.
            return !Thread.CurrentThread.IsThreadPoolThread;
        }
    }
}
