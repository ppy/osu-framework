// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        public double AverageFrameTime { get; private set; }

        public double FramesPerSecond { get; private set; }

        public virtual double CurrentTime { get; protected set; }

        public virtual double LastFrameTime { get; private set; }

        public double Rate => Source.Rate;

        protected double SourceTime => Source.CurrentTime;

        public double ElapsedFrameTime => CurrentTime - LastFrameTime;

        public bool IsRunning => Source?.IsRunning ?? false;

        double timeUntilNextCalculation;
        double timeSinceLastCalculation;
        int framesSinceLastCalculation;

        const int fps_calculation_interval = 250;

        public virtual void ProcessFrame()
        {
            //update framerate
            double decay = Math.Pow(0.05, ElapsedFrameTime);

            framesSinceLastCalculation++;
            timeUntilNextCalculation -= ElapsedFrameTime;
            timeSinceLastCalculation += ElapsedFrameTime;

            if (timeUntilNextCalculation <= 0)
            {
                timeUntilNextCalculation += fps_calculation_interval;

                if (framesSinceLastCalculation == 0)
                    FramesPerSecond = 0;
                else
                    FramesPerSecond = (int)Math.Ceiling(framesSinceLastCalculation * 1000f / timeSinceLastCalculation);
                timeSinceLastCalculation = framesSinceLastCalculation = 0;
            }

            AverageFrameTime = decay * AverageFrameTime + (1 - decay) * ElapsedFrameTime;

            LastFrameTime = CurrentTime;
            CurrentTime = SourceTime;
        }
    }
}
