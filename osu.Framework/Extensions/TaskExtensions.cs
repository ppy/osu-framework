// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Safe alternative to Task.Wait which ensures the calling thread is not a thread pool thread.
        /// </summary>
        public static void WaitSafely(this Task task)
        {
            if (Thread.CurrentThread.IsThreadPoolThread)
                throw new InvalidOperationException($"Can't use {nameof(WaitSafely)} from inside an async operation.");

#pragma warning disable RS0030
            task.Wait();
#pragma warning restore RS0030
        }

        /// <summary>
        /// Safe alternative to Task.Result which ensures the calling thread is not a thread pool thread.
        /// </summary>
        public static T WaitSafelyForResult<T>(this Task<T> task)
        {
            // We commonly access `.Result` from within `ContinueWith`, which is a safe usage (the task is guaranteed to be completed).
            // Unfortunately, the only way to allow these usages is to check whether the task is completed or not here.
            // This does mean that there could be edge cases where this safety is skipped (ie. if the majority of executions complete
            // immediately).
            if (Thread.CurrentThread.IsThreadPoolThread && !task.IsCompleted)
                throw new InvalidOperationException($"Can't use {nameof(WaitSafelyForResult)} from inside an async operation.");

#pragma warning disable RS0030
            return task.Result;
#pragma warning restore RS0030
        }
    }
}
