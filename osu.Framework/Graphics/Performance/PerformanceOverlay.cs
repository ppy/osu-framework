// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Performance
{
    class PerformanceOverlay : FlowContainer
    {
        public override void Load()
        {
            base.Load();

            Add(new FrameTimeDisplay(@"Input", Game.Host.InputMonitor));
            Add(new FrameTimeDisplay(@"Update", Game.Host.UpdateMonitor));
            Add(new FrameTimeDisplay(@"Draw", Game.Host.DrawMonitor));

            Direction = FlowDirection.VerticalOnly;
        }
    }
}
