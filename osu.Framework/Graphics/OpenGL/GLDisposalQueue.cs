// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace osu.Framework.Graphics.OpenGL
{
    /// <summary>
    /// Helper class used to manage GL disposals in a thread-safe manner.
    /// </summary>
    internal class GLDisposalQueue
    {
        private readonly List<PendingDisposal> newDisposals;
        private readonly List<PendingDisposal> pendingDisposals;

        public GLDisposalQueue()
        {
            newDisposals = new List<PendingDisposal>();
            pendingDisposals = new List<PendingDisposal>();
        }

        /// <summary>
        /// Schedules a new disposal action to be executed at a later point in time.
        /// This method can be called concurrently from multiple threads.
        /// By default the disposal will run <see cref="GLWrapper.MAX_DRAW_NODES"/> frames after enqueueing.
        /// </summary>
        /// <param name="disposalAction">The disposal action to be executed.</param>
        public void ScheduleDisposal(Action disposalAction)
        {
            lock (newDisposals)
                newDisposals.Add(new PendingDisposal(disposalAction));
        }

        /// <summary>
        /// Checks pending disposals and executes ones whose frame delay has expired.
        /// This method can NOT be called concurrently from multiple threads.
        /// </summary>
        public void CheckPendingDisposals()
        {
            lock (newDisposals)
            {
                pendingDisposals.AddRange(newDisposals);
                newDisposals.Clear();
            }

            for (var i = 0; i < pendingDisposals.Count; i++)
            {
                var item = pendingDisposals[i];

                if (item.RemainingFrameDelay-- == 0)
                {
                    item.Action();

                    Debug.Assert(i == 0);
                    pendingDisposals.RemoveAt(i--);
                }
            }
        }

        private class PendingDisposal
        {
            public int RemainingFrameDelay = GLWrapper.MAX_DRAW_NODES;

            public readonly Action Action;

            public PendingDisposal(Action action)
            {
                Action = action;
            }
        }
    }
}
