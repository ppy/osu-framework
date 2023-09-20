// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

namespace osu.Framework.Timing
{
    public class DecouplingFramedClock : IFrameBasedClock, ISourceChangeableClock
    {
        public virtual double Rate => FramedSourceClock.Rate;

        public virtual bool IsRunning => sourceIsRunning;

        public virtual double ElapsedFrameTime => FramedSourceClock.ElapsedFrameTime;

        public IClock Source { get; private set; }

        protected IFrameBasedClock FramedSourceClock;

        public virtual double CurrentTime => currentTime;

        private readonly FramedClock realtimeClock = new FramedClock(new StopwatchClock(true));

        private bool sourceIsRunning;

        private double currentTime;

        public DecouplingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(FramedSourceClock != null);
        }

        public void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            // We need a frame-based source to correctly process interpolation.
            // If the provided source is not already a framed clock, encapsulate it in one.
            FramedSourceClock = Source as IFrameBasedClock ?? new FramedClock(source);
        }

        public virtual void ProcessFrame()
        {
            realtimeClock.ProcessFrame();
            FramedSourceClock.ProcessFrame();

            sourceIsRunning = FramedSourceClock.IsRunning;

            if (FramedSourceClock.IsRunning)
            {
            }

            currentTime = FramedSourceClock.CurrentTime;
        }

        double IFrameBasedClock.FramesPerSecond => 0;
    }
}
