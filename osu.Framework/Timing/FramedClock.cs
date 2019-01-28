﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.TypeExtensions;
using System;

namespace osu.Framework.Timing
{
    /// <summary>
    /// Takes a clock source and separates time reading on a per-frame level.
    /// The CurrentTime value will only change on initial construction and whenever ProcessFrame is run.
    /// </summary>
    public class FramedClock : IFrameBasedClock
    {
        public IClock Source { get; }

        /// <summary>
        /// Construct a new FramedClock with an optional source clock.
        /// </summary>
        /// <param name="source">A source clock which will be used as the backing time source. If null, a StopwatchClock will be created. When provided, the CurrentTime of <see cref="source" /> will be transferred instantly.</param>
        public FramedClock(IClock source = null)
        {
            if (source != null)
            {
                CurrentTime = LastFrameTime = source.CurrentTime;
                Source = source;
            }
            else
                Source = new StopwatchClock(true);
        }

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };

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
            (Source as IFrameBasedClock)?.ProcessFrame();

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

            LastFrameTime = CurrentTime;
            CurrentTime = SourceTime;
        }

        public override string ToString() => $@"{GetType().ReadableName()} ({Math.Truncate(CurrentTime)}ms, {FramesPerSecond} FPS)";
    }
}
