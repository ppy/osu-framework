// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace osu.Framework.Threading
{
    /// <summary>
    /// Encapsulates tasks which were offloaded to background threads
    /// </summary>
    public class BackgroundTaskManager
    {
        private readonly List<Task> allTasks;

        public BackgroundTaskManager()
        {
            allTasks = new List<Task>();
        }

        public IEnumerable<Task> PendingTasks => allTasks.Where(t => t.Status == TaskStatus.Created);
        public IEnumerable<Task> RunningTasks => allTasks.Where(t => t.Status == TaskStatus.Running);
        public IEnumerable<Task> CompletedTasks => allTasks.Where(t => t.IsCompleted);
        public IEnumerable<Task> CancelledTasks => allTasks.Where(t => t.IsCanceled);

        public void Add(Task task) => allTasks.Add(task);
        public void Cancel(Task task) => throw new NotImplementedException();
        public void Cancel(IEnumerable<Task> task) => throw new NotImplementedException();
        public void WaitUnfinishedTasks() => Task.WaitAll(RunningTasks.ToArray());

        /// <summary>
        /// Creates and starts new task using provided delegate
        /// </summary>
        /// <param name="action">Delegate which will be executed in background</param>
        /// <param name="longRunning">true - create new thread, false - use pooled thread</param>
        /// <returns>Background task which was created</returns>
        public Task StartNew(Action action, bool longRunning = false)
        {
            var newTask = longRunning ? Task.Factory.StartNew(action, TaskCreationOptions.LongRunning) : Task.Factory.StartNew(action);
            allTasks.Add(newTask);
            return newTask;
        }
    }
}
