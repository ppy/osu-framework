// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public class LaggyFramedClock : FramedClock
    {
        public LaggyFramedClock(IClock? source = null)
            : base(source)
        {
        }

        private int lastSecond;

        public override void ProcessFrame()
        {
            int second = (int)Source.CurrentTime / 500;

            if (second != lastSecond)
            {
                lastSecond = second;
                base.ProcessFrame();
            }
        }
    }
}
