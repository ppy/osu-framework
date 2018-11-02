// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Performance
{
    internal abstract class PerformanceOverlayState : StateBase<PerformanceOverlay>
    {
        protected PerformanceOverlayState(PerformanceOverlay context)
            : base(context)
        {
        }

        public virtual void ToNone()
        {
            Context.FadeOut(100);
            foreach (FrameStatisticsDisplay d in Context.Children)
                d.State.ToNone();
            Context.State = new None(Context);
        }

        public virtual void ToMinimal()
        {
            Context.FadeIn(100);
            foreach (FrameStatisticsDisplay d in Context.Children)
                d.State.ToMinimal();
            Context.State = new Minimal(Context);
        }

        public virtual void ToFull()
        {
            Context.FadeIn(100);
            foreach (FrameStatisticsDisplay d in Context.Children)
                d.State.ToFull();
            Context.State = new Full(Context);
        }

        internal class None : PerformanceOverlayState
        {
            public None(PerformanceOverlay context)
                : base(context)
            {
            }

            public override void ToNone()
            {
            }
        }

        internal class Minimal : PerformanceOverlayState
        {
            public Minimal(PerformanceOverlay context)
                : base(context)
            {
            }

            public override void ToMinimal()
            {
            }
        }

        internal class Full : PerformanceOverlayState
        {
            public Full(PerformanceOverlay context)
                : base(context)
            {
            }

            public override void ToFull()
            {
            }
        }
    }
}
