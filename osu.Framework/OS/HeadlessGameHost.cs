// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Statistics;

namespace osu.Framework.OS
{
    /// <summary>
    /// A GameHost which doesn't require a graphical or sound device.
    /// </summary>
    public class HeadlessGameHost : BasicGameHost
    {
        public override GLControl GLControl => null;
        public override bool IsActive => true;
        public override TextInputSource TextInput => null;

        protected override void DrawFrame()
        {
            //we can't draw.
        }

        public override void Run()
        {
            while (!ExitRequested)
            {
                UpdateMonitor.NewFrame();

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                {
                    UpdateScheduler.Update();
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Update))
                {
                    UpdateSubTree();
                    using (var buffer = DrawRoots.Get(UsageType.Write))
                        buffer.Object = GenerateDrawNodeSubtree(buffer.Object);
                }

                using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Sleep))
                {
                    UpdateClock.ProcessFrame();
                }
            }
        }
    }
}
