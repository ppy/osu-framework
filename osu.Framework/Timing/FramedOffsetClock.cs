// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    /// <summary>
    /// A framed clock which allows an offset to be added or subtracted from an underlying source clock's time.
    /// </summary>
    public class FramedOffsetClock : FramedClock
    {
        public override double CurrentTime => base.CurrentTime + offset;

        private double offset;

        /// <summary>
        /// The offset to be applied.
        /// </summary>
        /// <remarks>
        /// A positive offset will move time forward.
        /// </remarks>
        public double Offset
        {
            get => offset;
            set
            {
                LastFrameTime += value - offset;
                offset = value;
            }
        }

        public FramedOffsetClock(IClock source, bool processSource = true)
            : base(source, processSource)
        {
        }
    }
}
