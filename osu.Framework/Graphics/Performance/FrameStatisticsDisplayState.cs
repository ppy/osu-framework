// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Performance
{
    internal abstract class FrameStatisticsDisplayState : StateBase<FrameStatisticsDisplay>
    {
        protected FrameStatisticsDisplayState(FrameStatisticsDisplay context)
            : base(context)
        {
        }

        public virtual void ToNone()
        {
            Context.Running = true;
            Context.Expanded = false;

            Context.State = new None(Context);
        }

        public virtual void ToMinimal()
        {
            Context.MainContainer.AutoSizeAxes = Axes.Both;

            Context.TimeBarsContainer.Hide();

            Context.LabelText.Origin = Anchor.CentreRight;
            Context.LabelText.Rotation = 0;

            Context.State = new Minimal(Context);
        }

        public virtual void ToFull()
        {
            Context.MainContainer.AutoSizeAxes = Axes.None;
            Context.MainContainer.Size = new Vector2(FrameStatisticsDisplay.WIDTH, FrameStatisticsDisplay.HEIGHT);

            Context.TimeBarsContainer.Show();

            Context.LabelText.Origin = Anchor.BottomCentre;
            Context.LabelText.Rotation = -90;

            Context.State = new Full(Context);
        }

        internal class None : FrameStatisticsDisplayState
        {
            public None(FrameStatisticsDisplay context)
                : base(context)
            {
            }

            public override void ToNone()
            {
            }
        }

        internal class Minimal : FrameStatisticsDisplayState
        {
            public Minimal(FrameStatisticsDisplay context)
                : base(context)
            {
            }

            public override void ToMinimal()
            {
            }
        }

        internal class Full : FrameStatisticsDisplayState
        {
            public Full(FrameStatisticsDisplay context)
                : base(context)
            {
            }

            public override void ToFull()
            {
            }
        }
    }
}
