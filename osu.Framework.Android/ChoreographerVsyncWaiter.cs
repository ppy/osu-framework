// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.OS;
using Android.Views;
using System;
using System.Threading;

namespace osu.Framework.Android
{
    public sealed class ChoreographerVsyncWaiter : IDisposable
    {
        private readonly HandlerThread thread;
        private readonly Handler handler;
        private readonly ManualResetEventSlim vsyncEvent = new ManualResetEventSlim(false);

        private Choreographer choreographer = null!;
        private readonly VsyncWaiterFrameCallback callback;
        private bool disposed;

        public ChoreographerVsyncWaiter()
        {
            thread = new HandlerThread("ChoreographerVsync");
            thread.Start();

            handler = new Handler(thread.Looper!);

            using var ready = new ManualResetEventSlim(false);

            handler.Post(() =>
            {
                choreographer = Choreographer.Instance!;
                ready.Set();
            });

            ready.Wait(30000);
            ready.Dispose();

            callback = new VsyncWaiterFrameCallback(vsyncEvent);
        }

        public void WaitForNextVsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            vsyncEvent.Reset();

            handler.Post(() =>
            {
                choreographer.PostFrameCallback(callback);
            });

            vsyncEvent.Wait(30000);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            thread.QuitSafely();
            thread.Join();

            callback.Dispose();
            vsyncEvent.Dispose();
        }

        private sealed class VsyncWaiterFrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
        {
            private readonly ManualResetEventSlim vsyncEvent;

            public VsyncWaiterFrameCallback(ManualResetEventSlim vsyncEvent)
            {
                this.vsyncEvent = vsyncEvent;
            }

            public void DoFrame(long _) => vsyncEvent.Set();
        }
    }
}
