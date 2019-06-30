// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// A component which allows registering and tracking of
    /// </summary>
    public interface IGlobalStatisticsTracker
    {
        void Register(IGlobalStatistic stat);
    }
}
