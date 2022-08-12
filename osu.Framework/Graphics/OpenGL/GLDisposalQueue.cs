// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.OpenGL
{
    /// <summary>
    /// Helper class used to manage GL disposals in a thread-safe manner.
    /// </summary>
    internal class GLDisposalQueue
    {
        private readonly List<IPendingDisposal> newDisposals;
        private readonly List<IPendingDisposal> pendingDisposals;

        public GLDisposalQueue()
        {
            newDisposals = new List<IPendingDisposal>();
            pendingDisposals = new List<IPendingDisposal>();
        }

        /// <summary>
        /// Schedules a new disposal action to be executed at a later point in time.
        /// This method can be called concurrently from multiple threads.
        /// By default the disposal will run <see cref="IRenderer.MAX_DRAW_NODES"/> frames after enqueueing.
        /// </summary>
        /// <param name="disposalAction">The disposal action to be executed.</param>
        /// <param name="target">The target.</param>
        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            lock (newDisposals)
                newDisposals.Add(new PendingDisposal<T>(disposalAction, target));
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

            // because disposals are added in batches every frame,
            // and each frame the remaining frame delay of all disposal tasks is decremented by 1,
            // all disposals that are executable this frame must be placed at the start of the list.
            // track the index of the last one, so we can clean them up in one fell swoop instead of as-we-go
            // (the latter approach can incur a quadratic time penalty).
            int lastExecutedDisposal = -1;

            for (int i = 0; i < pendingDisposals.Count; i++)
            {
                var item = pendingDisposals[i];

                if (item.RemainingFrameDelay-- == 0)
                {
                    item.Run();
                    lastExecutedDisposal = i;
                }
            }

            if (lastExecutedDisposal < 0)
                return;

            // note the signs - a 0 in the inner loop is a -1 here due to the postfix decrement.
            Debug.Assert(pendingDisposals[lastExecutedDisposal].RemainingFrameDelay < 0);
            Debug.Assert(lastExecutedDisposal + 1 == pendingDisposals.Count
                         || pendingDisposals[lastExecutedDisposal + 1].RemainingFrameDelay >= 0);

            pendingDisposals.RemoveRange(0, lastExecutedDisposal + 1);
        }

        private class PendingDisposal<T> : IPendingDisposal
        {
            public int RemainingFrameDelay { get; set; }

            private readonly Action<T> action;

            private readonly T target;

            public PendingDisposal(Action<T> disposeAction, T target)
            {
                action = disposeAction;
                this.target = target;
                RemainingFrameDelay = IRenderer.MAX_DRAW_NODES;
            }

            public void Run() => action(target);
        }

        private interface IPendingDisposal
        {
            void Run();

            int RemainingFrameDelay { get; set; }
        }
    }
}
