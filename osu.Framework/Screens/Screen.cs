// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Screens
{
    public class Screen : CompositeDrawable, IScreen
    {
        public bool ValidForResume { get; set; } = true;

        public bool ValidForPush { get; set; } = true;

        public sealed override bool RemoveWhenNotAlive => false;

        [Resolved]
        protected Game Game { get; private set; }

        public Screen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            base.UpdateClock(clock);
            if (Parent != null && !(Parent is ScreenStack))
                throw new InvalidOperationException($"Screens must always be added to a {nameof(ScreenStack)} (attempted to add {GetType()} to {Parent.GetType()})");
        }

#pragma warning disable CS0618
        public virtual void OnEntering(ScreenTransitionEvent e) => OnEntering(e.Last);

        public virtual bool OnExiting(ScreenExitEvent e) => OnExiting(e.Next);

        public virtual void OnResuming(ScreenTransitionEvent e) => OnResuming(e.Last);

        public virtual void OnSuspending(ScreenTransitionEvent e) => OnSuspending(e.Next);
#pragma warning restore CS0618

        [Obsolete("Override OnEntering(ScreenTransitionEvent) instead.")] // Can be removed 20221013
        public virtual void OnEntering(IScreen last)
        {
        }

        [Obsolete("Override OnExiting(ScreenExitEvent) instead.")] // Can be removed 20221013
        public virtual bool OnExiting(IScreen next) => false;

        [Obsolete("Override OnResuming(ScreenTransitionEvent) instead.")] // Can be removed 20221013
        public virtual void OnResuming(IScreen last)
        {
        }

        [Obsolete("Override OnSuspending(ScreenTransitionEvent) instead.")] // Can be removed 20221013
        public virtual void OnSuspending(IScreen next)
        {
        }
    }
}
