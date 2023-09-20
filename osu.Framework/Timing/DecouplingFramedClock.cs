// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Timing
{
    public class DecouplingFramedClock : IFrameBasedClock, ISourceChangeableClock, IAdjustableClock
    {
        public bool AllowDecoupling = true;

        public IClock Source { get; private set; }

        /// <summary>
        /// This clock is used when we are decoupling from the source, to figure out how much elapsed time has passed between frames.
        /// </summary>
        private readonly FramedClock realtimeReferenceClock = new FramedClock(new StopwatchClock(true));

        private IAdjustableClock adjustableSourceClock;

        private bool isRunning;

        public DecouplingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(adjustableSourceClock != null);
        }

        public void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            if (Source is not IAdjustableClock adjustableSource)
                throw new ArgumentException($"Clock must be of type {nameof(IAdjustableClock)}");

            adjustableSourceClock = adjustableSource;
        }

        public virtual void ProcessFrame()
        {
            updateRealtimeReference();

            double lastTime = CurrentTime;

            if (Source.IsRunning || !AllowDecoupling)
            {
                ElapsedFrameTime = Source.CurrentTime - lastTime;
                CurrentTime = Source.CurrentTime;
            }
            else
            {
                if (isRunning)
                    CurrentTime += realtimeReferenceClock.ElapsedFrameTime * Rate;
            }
        }

        private void updateRealtimeReference()
        {
            ((StopwatchClock)realtimeReferenceClock.Source).Rate = Source.Rate;
            realtimeReferenceClock.ProcessFrame();
        }

        #region IAdjustableClock implementation

        public void Reset()
        {
            adjustableSourceClock.Reset();
        }

        public void Start()
        {
            adjustableSourceClock.Start();
        }

        public void Stop()
        {
            adjustableSourceClock.Stop();
        }

        public bool Seek(double position)
        {
            if (adjustableSourceClock.Seek(position))
                return true;

            if (!AllowDecoupling)
                return false;

            CurrentTime = position;
            return true;
        }

        public void ResetSpeedAdjustments() => adjustableSourceClock.ResetSpeedAdjustments();

        public virtual double Rate
        {
            get => adjustableSourceClock.Rate;
            set => adjustableSourceClock.Rate = value;
        }

        #endregion

        # region IFrameBasedClock delegation

        public double ElapsedFrameTime { get; private set; }

        public bool IsRunning
        {
            get
            {
                // Always immediately use the source clock's running state if it's running.
                if (Source.IsRunning || !AllowDecoupling)
                    return isRunning = Source.IsRunning;

                return isRunning;
            }
        }

        public virtual double CurrentTime { get; private set; }
        double IFrameBasedClock.FramesPerSecond => 0;

        #endregion
    }
}
