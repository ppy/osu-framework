// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    internal class TestClock : IAdjustableClock
    {
        public double CurrentTime { get; set; }
        public double Rate { get; set; } = 1;

        public bool IsRunning { get; private set; }

        public void Reset()
        {
            CurrentTime = 0;
            IsRunning = false;
        }

        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;

        public virtual bool Seek(double position)
        {
            CurrentTime = position;
            return true;
        }

        public void ResetSpeedAdjustments() => throw new NotImplementedException();
    }
}
