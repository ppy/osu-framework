// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    /// <summary>
    /// A completely manual clock implementation. Everything is settable.
    /// </summary>
    public class ManualClock : IClock
    {
        public double CurrentTime { get; set; }
        public double Rate { get; set; }
        public bool IsRunning { get; set; }
    }
}
