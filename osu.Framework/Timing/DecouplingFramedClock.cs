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

        /// <summary>
        /// This clock is used to set final outputs on.
        /// </summary>
        private readonly ManualFramedClock outputClock = new ManualFramedClock();

        private IFrameBasedClock framedSourceClock;
        private IAdjustableClock adjustableSourceClock;

        public DecouplingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(framedSourceClock != null);
            Debug.Assert(adjustableSourceClock != null);
        }

        public void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            if (Source is not IAdjustableClock adjustableSource)
                throw new ArgumentException($"Clock must be of type {nameof(IAdjustableClock)}");

            adjustableSourceClock = adjustableSource;

            // We need a frame-based source to correctly process interpolation.
            // If the provided source is not already a framed clock, encapsulate it in one.
            framedSourceClock = Source as IFrameBasedClock ?? new FramedClock(source);
        }

        public virtual void ProcessFrame()
        {
            updateRealtimeReference();

            framedSourceClock.ProcessFrame();

            if (AllowDecoupling)
            {
            }

            outputClock.Rate = framedSourceClock.Rate;
            outputClock.CurrentTime = framedSourceClock.CurrentTime;
            outputClock.IsRunning = framedSourceClock.IsRunning;
            outputClock.ElapsedFrameTime = framedSourceClock.ElapsedFrameTime;
        }

        private void updateRealtimeReference()
        {
            ((StopwatchClock)realtimeReferenceClock.Source).Rate = framedSourceClock.Rate;
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
            return adjustableSourceClock.Seek(position);
        }

        public void ResetSpeedAdjustments() => adjustableSourceClock.ResetSpeedAdjustments();

        public virtual double Rate
        {
            get => adjustableSourceClock.Rate;
            set => adjustableSourceClock.Rate = value;
        }

        #endregion

        # region IFrameBasedClock delegation

        public double ElapsedFrameTime => outputClock.ElapsedFrameTime;
        public bool IsRunning => outputClock.IsRunning;
        public virtual double CurrentTime => outputClock.CurrentTime;
        double IFrameBasedClock.FramesPerSecond => 0;

        #endregion
    }
}
