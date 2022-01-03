// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.Extensions
{
    public static class TaskExtensions
    {
        public static void WaitSafely(this Task task)
        {
            if (Thread.CurrentThread.IsThreadPoolThread)
                throw new InvalidOperationException($"Can't use {nameof(WaitSafely)} from inside an async operation.");

#pragma warning disable RS0030
            task.Wait();
#pragma warning restore RS0030
        }
    }
}
