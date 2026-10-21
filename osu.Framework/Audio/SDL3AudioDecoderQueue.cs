// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace osu.Framework.Audio
{
    public class SDL3AudioDecoderQueue
    {
        public interface IWorker
        {
            /// <summary>
            /// Try decoding a piece of data.
            /// </summary>
            /// <returns>false if it is done decoding. This won't be called after it returns false.</returns>
            bool DoWork();
        }

        public static readonly SDL3AudioDecoderQueue INSTANCE = new SDL3AudioDecoderQueue();

        private readonly LinkedList<IWorker> workers = new LinkedList<IWorker>();
        private readonly object lockHandle = new object();
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly AutoResetEvent waitHandle = new AutoResetEvent(false);
        private readonly ConcurrentQueue<IWorker> queue = new ConcurrentQueue<IWorker>();
        private Thread? thread;

        private SDL3AudioDecoderQueue()
        {
        }

        ~SDL3AudioDecoderQueue()
        {
            tokenSource.Cancel();
            waitHandle.Set();
            thread?.Join();
            tokenSource.Dispose();
            waitHandle.Dispose();
        }

        private void loop()
        {
            var token = tokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var enqueued))
                    workers.AddFirst(enqueued);

                if (workers.Count == 0)
                {
                    waitHandle.WaitOne();
                }
                else
                {
                    var node = workers.First;

                    while (node != null)
                    {
                        var next = node.Next;
                        var worker = node.Value;

                        if (!worker.DoWork())
                            workers.Remove(node);

                        node = next;
                    }
                }
            }
        }

        public void Enqueue(IWorker worker)
        {
            queue.Enqueue(worker);

            if (thread == null)
            {
                lock (lockHandle)
                {
                    if (thread == null)
                    {
                        thread = new Thread(loop)
                        {
                            IsBackground = true
                        };
                        thread.Start();
                    }
                }
            }

            waitHandle.Set();
        }
    }
}
