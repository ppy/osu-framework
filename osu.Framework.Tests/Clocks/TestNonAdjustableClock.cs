// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    internal class TestNonAdjustableClock : IClock
    {
        public double CurrentTime { get; set; }
        public double Rate { get; set; } = 1;

        public bool IsRunning => true;
    }
}
