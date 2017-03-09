// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using System;

namespace osu.Framework.Timing
{
    /// <summary>
    /// Takes a clock source and separates time reading on a per-frame level.
    /// The CurrentTime value will only update when ProcessFrame is run.
    /// </summary>
    public class FramedClock : IFrameBasedClock
    {
        public IClock Source { get; }

        public FramedClock()
            : this(new StopwatchClock(true))
        {
        }

        public FramedClock(IClock source)
        {
            Source = source;
        }

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };

        public double AverageFrameTime { get; private set; }

        public double FramesPerSecond { get; private set; }

        public virtual double CurrentTime { get; protected set; }

        protected virtual double LastFrameTime { get; set; }

        public double Rate => Source.Rate;

        protected double SourceTime => Source.CurrentTime;

        public double ElapsedFrameTime => CurrentTime - LastFrameTime;

        public bool IsRunning => Source?.IsRunning ?? false;

        private double timeUntilNextCalculation;
        private double timeSinceLastCalculation;
        private int framesSinceLastCalculation;

        private const int fps_calculation_interval = 250;

        public virtual void ProcessFrame()
        {
            if (timeUntilNextCalculation <= 0)
            {
                timeUntilNextCalculation += fps_calculation_interval;

                if (framesSinceLastCalculation == 0)
                    FramesPerSecond = 0;
                else
                    FramesPerSecond = (int)Math.Ceiling(framesSinceLastCalculation * 1000f / timeSinceLastCalculation);
                timeSinceLastCalculation = framesSinceLastCalculation = 0;
            }

            framesSinceLastCalculation++;
            timeUntilNextCalculation -= ElapsedFrameTime;
            timeSinceLastCalculation += ElapsedFrameTime;

            AverageFrameTime = Interpolation.Damp(AverageFrameTime, ElapsedFrameTime, 0.01, Math.Abs(ElapsedFrameTime) / 1000);

            LastFrameTime = CurrentTime;
            CurrentTime = SourceTime;
        }
    }
}
