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
        private static readonly object instance_lock = new object();
        private static BackgroundTaskManager instance;
        private readonly List<Task> allTasks;

        private BackgroundTaskManager()
        {
            allTasks = new List<Task>();
        }

        public static BackgroundTaskManager Instance
        {
            get
            {
                lock (instance_lock)
                    return instance ?? (instance = new BackgroundTaskManager());
            }
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
