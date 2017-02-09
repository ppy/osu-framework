// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;

namespace osu.Framework.Threading
{
    public class SleepHandle : IDisposable
    {
        private AutoResetEvent sleepTimeOut;
        private AutoResetEvent taskDone;
        private object locker = new object();
        private Action task;
        private bool cleanLater;
        internal bool IsSleeping { get; private set; }

        public SleepHandle()
        {
            sleepTimeOut = new AutoResetEvent(false);
            taskDone = new AutoResetEvent(false);
        }

        //sleep until we get disrupted and have a task
        public void Sleep(int milliseconds)
        {
            IsSleeping = true;
            //we use datetime as it's a lot faster than the stopwatch and we don't need the accuracy.
            DateTime before = DateTime.Now;
            do
            {
                if (cleanLater)
                {
                    taskDone.Set();
                    cleanLater = false;
                }
                sleepTimeOut.WaitOne(milliseconds);
                if (task != null)
                    executeTask(false);
            } while ((DateTime.Now - before).TotalMilliseconds < milliseconds);
            IsSleeping = false;
            // in case task was trying to be inoked right after a an other task got executed
            if (task != null)
                executeTask(true);
        }

        private void executeTask(bool cleanDirectly)
        {
            task.Invoke();
            task = null;
            if (cleanDirectly)
                taskDone.Set();
            else
                cleanLater = true;
        }

        public void Invoke(Action task)
        {
            lock (locker)
            {
                if (this.task != null)
                    throw new InvalidOperationException();
                this.task = task;
                //disrupt time handle
                sleepTimeOut.Set();
                taskDone.WaitOne();
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            sleepTimeOut?.Dispose();
            sleepTimeOut = null;
            taskDone?.Dispose();
            taskDone = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
