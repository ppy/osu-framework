// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public class LaggyFramedClock : FramedClock
    {
        private readonly int frameSkip;

        private int currentFrame;

        public LaggyFramedClock(int frameSkip, IClock? source = null)
            : base(source)
        {
            this.frameSkip = frameSkip;
        }

        public override void ProcessFrame()
        {
            if (currentFrame++ % frameSkip == 0)
                base.ProcessFrame();
        }
    }
}
