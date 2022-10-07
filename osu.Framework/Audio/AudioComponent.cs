// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A base class for audio components which offers audio thread deferring, disposal and basic update logic.
    /// </summary>
    public abstract class AudioComponent : IDisposable, IUpdateable
    {
        /// <summary>
        /// Audio operations will be run on a separate dedicated thread, so we need to schedule any audio API calls using this queue.
        /// </summary>
        protected ConcurrentQueue<Task> PendingActions = new ConcurrentQueue<Task>();

        private bool acceptingActions = true;

        /// <summary>
        /// Whether an audio thread specific action can be performed inline.
        /// </summary>
        protected bool CanPerformInline =>
            ThreadSafety.IsAudioThread || (ThreadSafety.ExecutionMode == ExecutionMode.SingleThread && ThreadSafety.IsUpdateThread);

        /// <summary>
        /// Enqueues an action to be performed on the audio thread.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>A task which can be used for continuation logic. May return a <see cref="Task.CompletedTask"/> if called while already on the audio thread.</returns>
        protected Task EnqueueAction(Action action)
        {
            if (CanPerformInline)
            {
                action();
                return Task.CompletedTask;
            }

            if (!acceptingActions)
                // we don't want consumers to block on operations after we are disposed.
                return Task.CompletedTask;

            var task = new Task(action);
            PendingActions.Enqueue(task);
            return task;
        }

        /// <summary>
        /// Run each loop of the audio thread's execution after queued actions are completed to allow components to perform any additional operations.
        /// </summary>
        protected virtual void UpdateState()
        {
        }

        /// <summary>
        /// Run each loop of the audio thread's execution, after <see cref="UpdateState"/> as a way to update any child components.
        /// </summary>
        protected virtual void UpdateChildren()
        {
        }

        /// <summary>
        /// Updates this audio component. Always runs on the audio thread.
        /// </summary>
        public void Update()
        {
            ThreadSafety.EnsureNotUpdateThread();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not update disposed audio components.");

            FrameStatistics.Add(StatisticsCounterType.TasksRun, PendingActions.Count);
            FrameStatistics.Increment(StatisticsCounterType.Components);

            while (!IsDisposed && PendingActions.TryDequeue(out Task task))
                task.RunSynchronously();

            if (!IsDisposed)
                UpdateState();

            UpdateChildren();
        }

        /// <summary>
        /// Whether this component has completed playback and is in a stopped state.
        /// </summary>
        public virtual bool HasCompleted => !IsAlive;

        /// <summary>
        /// When false, this component has completed all processing and is ready to be removed from its parent.
        /// </summary>
        public virtual bool IsAlive => !IsDisposed;

        /// <summary>
        /// Whether this component has finished loading its resources.
        /// </summary>
        public virtual bool IsLoaded => true;

        #region IDisposable Support

        protected volatile bool IsDisposed;

        public void Dispose()
        {
            acceptingActions = false;
            PendingActions.Enqueue(new Task(() => Dispose(true)));

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        #endregion
    }
}
