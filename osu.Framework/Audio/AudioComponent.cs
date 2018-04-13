// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    public class AudioComponent : IDisposable, IUpdateable
    {
        /// <summary>
        /// Audio operations will be run on a separate dedicated thread, so we need to schedule any audio API calls using this queue.
        /// </summary>
        protected ConcurrentQueue<Task> PendingActions = new ConcurrentQueue<Task>();

        protected Task EnqueueAction(Action action)
        {
            var task = new Task(action);

            if (ThreadSafety.IsAudioThread)
            {
                task.RunSynchronously();
                return task;
            }

            if (!acceptingActions)
                // we don't want consumers to block on operations after we are disposed.
                return Task.CompletedTask;

            PendingActions.Enqueue(task);
            return task;
        }

        private bool acceptingActions = true;

        ~AudioComponent()
        {
            Dispose(false);
        }

        /// <summary>
        /// Run each loop of the audio thread after queued actions to allow components to update anything they need to.
        /// </summary>
        protected virtual void UpdateState()
        {
        }

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
        /// This component has completed playback and is now in a stopped state.
        /// </summary>
        public virtual bool HasCompleted => !IsAlive;

        /// <summary>
        /// This component has completed all processing and is ready to be removed from its parent.
        /// </summary>
        public virtual bool IsAlive => !IsDisposed;

        public virtual bool IsLoaded => true;

        #region IDisposable Support

        protected volatile bool IsDisposed; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        public virtual void Dispose()
        {
            acceptingActions = false;
            PendingActions.Enqueue(new Task(() => Dispose(true)));
        }

        #endregion
    }
}
