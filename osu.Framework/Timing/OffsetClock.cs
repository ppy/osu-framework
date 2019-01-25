// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    public class OffsetClock : IClock
    {
        protected IClock Source;

        public double Offset;

        public double CurrentTime => Source.CurrentTime + Offset;

        public double Rate => Source.Rate;

        public bool IsRunning => Source.IsRunning;

        public OffsetClock(IClock source)
        {
            Source = source;
        }
    }
}
