// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Threading
{
    public class ScheduledDelegateWithData<T> : ScheduledDelegate
    {
        public new readonly Action<T> Task;

        public ScheduledDelegateWithData(Action<T> task, T data, double executionTime = 0, double repeatInterval = -1)
            : base(() => task(data), executionTime, repeatInterval)
        {
            Task = task;
        }
    }
}
