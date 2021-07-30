// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Graphics;

#nullable enable

namespace osu.Framework.Threading
{
    /// <summary>
    /// A synchronisation context which posts all continuatiuons to a scheduler instance.
    /// </summary>
    internal class DrawableSynchronizationContext : SynchronizationContext
    {
        private readonly Drawable drawable;

        public DrawableSynchronizationContext(Drawable drawable)
        {
            this.drawable = drawable;
        }

        public override void Post(SendOrPostCallback d, object? state) => drawable.Scheduler.Add(() => d(state));
    }
}
